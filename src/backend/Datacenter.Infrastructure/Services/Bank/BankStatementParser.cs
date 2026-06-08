using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Datacenter.Infrastructure.Services.Bank;

/// <summary>
/// แปลงไฟล์ statement → บรรทัดมาตรฐาน.
/// - PDF: coordinate-based (PdfPig) auto-detect SCB/KBANK/TTB → group แถวด้วย y, แยกคอลัมน์ด้วยช่วง x,
///   ระบุทิศ (ฝาก/ถอน) จาก balance-delta แล้ว self-check (ยอดต้น + Σฝาก − Σถอน = ยอดปลาย).
/// - Excel/CSV: คอลัมน์คงที่ A=วันที่ B=รายละเอียด C=ถอน D=ฝาก E=ยอดคงเหลือ (data เริ่มแถว 2).
/// </summary>
public class BankStatementParser : IBankStatementParser
{
    private static readonly Regex Num = new(@"^-?\d{1,3}(,\d{3})*\.\d{2}$", RegexOptions.Compiled);
    private static readonly string[] OpeningKeywords =
        { "BROUGHT FORWARD", "ยอดยกมา", "ยอดเงินยกมา", "ยอดคงเหลือยกมา" };

    public BankStatementParseResult Parse(byte[] content, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ParsePdf(content),
            ".xlsx" or ".xls" => ParseExcel(content),
            ".csv" => ParseCsv(content),
            _ => throw new InvalidOperationException($"ไม่รองรับไฟล์ชนิด {ext} (รองรับ .pdf, .xlsx, .csv)")
        };
    }

    public byte[] BuildTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Statement");
        var headers = new[] { "วันที่ (dd/MM/yyyy)", "รายละเอียด", "เงินออก (ถอน)", "เงินเข้า (ฝาก)", "ยอดคงเหลือ" };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        // ตัวอย่าง 1 แถว
        ws.Cell(2, 1).Value = "01/05/2026";
        ws.Cell(2, 2).Value = "โอนเงินรับจากลูกค้า";
        ws.Cell(2, 3).Value = 0;
        ws.Cell(2, 4).Value = 5000;
        ws.Cell(2, 5).Value = 105000;
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─────────────────────────── PDF ───────────────────────────

    private sealed record BankConfig(string Code, Regex DateRegex, string DateFormat, double MergeTol);

    private static readonly BankConfig[] Configs =
    {
        // แยกธนาคารด้วยตัวคั่นวันที่ (exclusive): SCB=/ KBANK=- TTB=.
        // ใช้ prefix match (ไม่ anchor ท้าย) เผื่อ PdfPig รวม date+time เป็น token เดียว เช่น "30-04-2602:49"
        new("SCB",   new(@"^\d{2}/\d{2}/\d{2}"),   "dd/MM/yy",   4.0),
        new("KBANK", new(@"^\d{2}-\d{2}-\d{2}"),   "dd-MM-yy",   4.0),
        new("TTB",   new(@"^\d{2}\.\d{2}\.\d{4}"), "dd.MM.yyyy", 4.0),
    };

    private sealed class VLine { public int Page; public double Y; public List<(double X, string T)> W = new(); }

    private BankStatementParseResult ParsePdf(byte[] content)
    {
        using var doc = PdfDocument.Open(content);
        var allText = new System.Text.StringBuilder();
        var lines = new List<VLine>();

        foreach (var page in doc.GetPages())
        {
            // group words into visual lines (y top-down = Height - Top)
            var words = page.GetWords()
                .Select(w => (X: w.BoundingBox.Left, Y: page.Height - w.BoundingBox.Top, T: w.Text))
                .Where(t => !string.IsNullOrWhiteSpace(t.T))
                .ToList();
            foreach (var w in words) allText.Append(w.T).Append(' ');

            // จับ word เป็น "บรรทัด" ด้วย adaptive clustering: word เข้าบรรทัดเดิมถ้า Y ห่าง ≤ 2pt
            // (แยกแถวที่ชิดกัน เช่น KBANK วันเดียวกันหลายรายการ ได้ดีกว่า bucket หยาบ)
            VLine? acc = null;
            foreach (var w in words.OrderBy(w => w.Y).ThenBy(w => w.X))
            {
                if (acc is null || Math.Abs(w.Y - acc.Y) > 3.0)
                {
                    acc = new VLine { Page = page.Number, Y = w.Y };
                    lines.Add(acc);
                }
                acc.W.Add((w.X, w.T));
            }
        }
        foreach (var l in lines) l.W = l.W.OrderBy(w => w.X).ToList();
        lines = lines.OrderBy(l => l.Page).ThenBy(l => l.Y).ToList();

        var cfg = DetectBank(lines);
        if (cfg is null)
            return new BankStatementParseResult("UNKNOWN", null, null, null, 0, 0, 0, false,
                Array.Empty<ParsedStatementLine>(), "ไม่รองรับการ parse PDF ของธนาคารนี้ — โปรดใช้เทมเพลต Excel/CSV");

        decimal? opening = null, pendingSingle = null;
        bool seenTxn = false;
        var raw = new List<(DateTime Date, decimal Amount, decimal Balance, string Desc)>();
        // cur: วันที่ + ตัวเลขสะสม (x,value) + คำอธิบาย
        (DateTime Date, List<(double X, decimal V)> Nums, string Desc)? cur = null;
        double curY = 0;

        void Flush()
        {
            if (cur is { } c && c.Nums.Count >= 2)
            {
                var ns = c.Nums.OrderBy(n => n.X).ToList();
                raw.Add((c.Date, ns[^2].V, ns[^1].V, c.Desc)); // ท้ายสุด = ยอดคงเหลือ, ก่อนหน้า = จำนวนเงิน
            }
            cur = null;
        }

        foreach (var line in lines)
        {
            var joined = string.Join(' ', line.W.Select(w => w.T));
            var nums = line.W.Where(w => Num.IsMatch(w.T))
                .Select(w => (w.X, V: decimal.Parse(w.T.Replace(",", ""), CultureInfo.InvariantCulture))).ToList();
            bool isKeyword = OpeningKeywords.Any(k => joined.Contains(k, StringComparison.OrdinalIgnoreCase));
            // หา date token ในคอลัมน์ซ้าย (x < 120) — ธุรกรรมจริงวันที่อยู่ซ้ายสุด,
            // ส่วนบรรทัดสรุปมีวันที่ฝังในข้อความ (x มาก) จะไม่ถูกจับ; เผื่อ PdfPig merge footer (x น้อย) เข้ามา
            var dateTok = line.W.Where(w => w.X < 120 && cfg.DateRegex.IsMatch(w.T))
                .Select(w => w.T).FirstOrDefault();

            if (isKeyword)
            {
                if (opening is null && nums.Count > 0) opening = nums.OrderBy(n => n.X).Last().V;
                Flush(); // page-boundary / brought-forward marker — ไม่ใช่ธุรกรรม
                continue;
            }

            if (dateTok is not null)
            {
                Flush();
                var dt = ParseDate(cfg.DateRegex.Match(dateTok).Value, cfg.DateFormat);
                cur = dt is null ? null : (dt.Value, new List<(double, decimal)>(nums), joined);
                curY = line.Y;
                seenTxn = true;
            }
            else if (cur is not null && line.Y - curY <= cfg.MergeTol && nums.Count < 3)
            {
                cur.Value.Nums.AddRange(nums);
            }
            else if (!seenTxn && opening is null && nums.Count == 1)
            {
                pendingSingle = nums[0].V; // ผู้สมัครยอดยกมา (บรรทัดก่อนธุรกรรมแรก)
            }
        }
        Flush();

        opening ??= pendingSingle;

        // direction via balance-delta
        decimal open = opening ?? 0m;
        decimal prev = open, dep = 0m, wd = 0m;
        var outLines = new List<ParsedStatementLine>(raw.Count);
        bool allDeltaOk = true;
        foreach (var r in raw)
        {
            decimal delta = Math.Round(r.Balance - prev, 2);
            decimal amt = Math.Round(r.Amount, 2);
            if (Math.Abs(Math.Abs(delta) - amt) > 0.009m) allDeltaOk = false;
            bool isDeposit = delta >= 0;
            outLines.Add(new ParsedStatementLine(r.Date, CleanDesc(r.Desc, cfg),
                isDeposit ? 0m : amt, isDeposit ? amt : 0m, r.Balance));
            if (isDeposit) dep += amt; else wd += amt;
            prev = r.Balance;
        }

        decimal closing = raw.Count > 0 ? raw[^1].Balance : open;
        decimal computed = Math.Round(open + dep - wd, 2);
        bool ok = opening is not null && raw.Count > 0 && allDeltaOk && Math.Abs(computed - closing) < 0.01m;

        var (accNo, ps, pe) = ParseHeader(allText.ToString());
        string? warn = ok ? null
            : opening is null ? "หายอดยกมาไม่พบ — โปรดตรวจ/แก้บรรทัดใน preview"
            : "balance self-check ไม่ผ่าน — โปรดตรวจ/แก้บรรทัดใน preview";

        return new BankStatementParseResult(cfg.Code, accNo, ps, pe, open, closing, computed, ok, outLines, warn);
    }

    /// <summary>เลือกธนาคารจากความถี่ของรูปแบบวันที่ (ตัวคั่น / - . แยกแบงก์ได้ชัด ไม่ปนกับชื่อคู่ค้า)</summary>
    private static BankConfig? DetectBank(List<VLine> lines)
    {
        BankConfig? best = null; int bestCount = 0;
        foreach (var cfg in Configs)
        {
            int c = lines.Count(l => l.W.Any(w => w.X < 120 && cfg.DateRegex.IsMatch(w.T)));
            if (c > bestCount) { bestCount = c; best = cfg; }
        }
        return bestCount > 0 ? best : null;
    }

    private static DateTime? ParseDate(string s, string fmt)
        => DateTime.TryParseExact(s, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static string? CleanDesc(string joined, BankConfig cfg)
    {
        // ตัดวันที่/เวลา/ตัวเลขจำนวนเงินออก เหลือคำอธิบาย (best-effort)
        var parts = joined.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !cfg.DateRegex.IsMatch(p) && !Num.IsMatch(p)
                     && !Regex.IsMatch(p, @"^\d{2}:\d{2}(:\d{2})?$"))
            .ToList();
        var d = string.Join(' ', parts).Trim();
        return d.Length == 0 ? null : (d.Length > 250 ? d[..250] : d);
    }

    private static (string? AccountNo, DateTime? Start, DateTime? End) ParseHeader(string text)
    {
        string? acc = null;
        var am = Regex.Match(text, @"\d{3}-\d-\d{5}-\d");
        if (am.Success) acc = am.Value;
        else { var am2 = Regex.Match(text, @"\d{3}\s\d\s\d{5}\s\d"); if (am2.Success) acc = am2.Value; }

        DateTime? ps = null, pe = null;
        var pm = Regex.Match(text, @"(\d{2}[/.]\d{2}[/.]\d{4})\s*-\s*(\d{2}[/.]\d{2}[/.]\d{4})");
        if (pm.Success)
        {
            ps = TryDate(pm.Groups[1].Value);
            pe = TryDate(pm.Groups[2].Value);
        }
        return (acc, ps, pe);
    }

    private static DateTime? TryDate(string s)
    {
        foreach (var f in new[] { "dd/MM/yyyy", "dd.MM.yyyy" })
            if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
        return null;
    }

    // ─────────────────────────── Excel / CSV (เทมเพลต) ───────────────────────────

    private BankStatementParseResult ParseExcel(byte[] content)
    {
        using var ms = new MemoryStream(content);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        int last = ws.LastRowUsed()?.RowNumber() ?? 0;
        var rows = new List<ParsedStatementLine>();
        for (int r = 2; r <= last; r++)
        {
            var dateCell = ws.Cell(r, 1);
            if (dateCell.IsEmpty()) continue;
            DateTime? dt = null;
            if (dateCell.TryGetValue<DateTime>(out var dv)) dt = dv;
            else dt = TryDateFlexible(dateCell.GetString().Trim());
            if (dt is null) continue;
            var desc = ws.Cell(r, 2).GetString().Trim();
            decimal wd = DecCell(ws.Cell(r, 3)), dep = DecCell(ws.Cell(r, 4));
            decimal? bal = ws.Cell(r, 5).IsEmpty() ? null : DecCell(ws.Cell(r, 5));
            if (wd == 0 && dep == 0) continue;
            rows.Add(new ParsedStatementLine(dt.Value, string.IsNullOrEmpty(desc) ? null : desc, wd, dep, bal));
        }
        return BuildTabularResult("EXCEL", rows);
    }

    private BankStatementParseResult ParseCsv(byte[] content)
    {
        var text = new System.Text.StringBuilder();
        // strip UTF-8 BOM
        int start = content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF ? 3 : 0;
        var s = System.Text.Encoding.UTF8.GetString(content, start, content.Length - start);
        var rows = new List<ParsedStatementLine>();
        var allLines = s.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        for (int i = 1; i < allLines.Length; i++) // skip header
        {
            var line = allLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = SplitCsv(line);
            if (f.Count < 4) continue;
            var dt = TryDateFlexible(f[0].Trim());
            if (dt is null) continue;
            decimal wd = ParseDec(f[2]), dep = ParseDec(f[3]);
            decimal? bal = f.Count > 4 && !string.IsNullOrWhiteSpace(f[4]) ? ParseDec(f[4]) : null;
            if (wd == 0 && dep == 0) continue;
            rows.Add(new ParsedStatementLine(dt.Value, string.IsNullOrWhiteSpace(f[1]) ? null : f[1].Trim(), wd, dep, bal));
        }
        return BuildTabularResult("CSV", rows);
    }

    private static BankStatementParseResult BuildTabularResult(string code, List<ParsedStatementLine> rows)
    {
        decimal dep = rows.Sum(r => r.Deposit), wd = rows.Sum(r => r.Withdrawal);
        var ps = rows.Count > 0 ? rows.Min(r => r.Date) : (DateTime?)null;
        var pe = rows.Count > 0 ? rows.Max(r => r.Date) : (DateTime?)null;
        decimal closing = rows.LastOrDefault(r => r.Balance is not null)?.Balance ?? 0m;
        // เทมเพลตไม่บังคับ balance ทุกแถว → opening/closing best-effort, self-check ปล่อยให้ผู้ใช้กรอกยอดเอง
        return new BankStatementParseResult(code, null, ps, pe, 0m, closing, Math.Round(dep - wd, 2),
            false, rows, "นำเข้าจากเทมเพลต — โปรดตรวจ/กรอกยอดต้น-ปลายงวดเพื่อกระทบยอด");
    }

    private static List<string> SplitCsv(string line)
    {
        var res = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool q = false;
        foreach (var ch in line)
        {
            if (ch == '"') q = !q;
            else if (ch == ',' && !q) { res.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        res.Add(sb.ToString());
        return res;
    }

    private static decimal DecCell(IXLCell c)
    {
        if (c.IsEmpty()) return 0m;
        if (c.TryGetValue<decimal>(out var d)) return d;
        return ParseDec(c.GetString());
    }

    private static decimal ParseDec(string s)
        => decimal.TryParse(s.Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0m;

    private static DateTime? TryDateFlexible(string s)
    {
        foreach (var f in new[] { "dd/MM/yyyy", "dd-MM-yyyy", "dd.MM.yyyy", "yyyy-MM-dd", "d/M/yyyy" })
            if (DateTime.TryParseExact(s, f, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var g) ? g : null;
    }
}
