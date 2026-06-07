using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.FinancialStatement.Services;

/// <summary>
/// Engine สร้างส่วน "data-binding" ของหมายเหตุประกอบงบการเงิน (NOTE2) — pure, ไม่แตะ DB.
///
/// แต่ละหมายเหตุแบบตาราง (6.x) = breakdown ของบรรทัดงบ (RefCode) หนึ่งบรรทัดขึ้นไป
/// แสดงบัญชีย่อยพร้อมยอดปีปัจจุบัน/ปีก่อน. ใช้ sign convention เดียวกับ FinancialStatementEngine:
///   สินทรัพย์ (A) / ค่าใช้จ่าย (X) : คงเครื่องหมายธรรมชาติ (debit−credit)
///   หนี้สิน (L) / ทุน (E) / รายได้ (I) : กลับเครื่องหมาย
///
/// แหล่งยอด: ใช้ยอด "สะสมถึงสิ้นปี" (cumulative) ทั้งงบฐานะและงบกำไรขาดทุน — ตรงกับหลักของ
/// GetProfitLossQueryHandler: Express ทับยอด P&L งวดที่ปลายปี + opening ล้างยอดปีก่อน ทำให้
/// ยอดสะสมถึงสิ้นปี = ยอดงวดนั้นที่ถูกต้อง. ใช้ฐานเดียวกันเพื่อให้หมายเหตุ "ลงตรง" กับงบที่แสดง.
/// </summary>
public static class NotesEngine
{
    private sealed record NoteDef(
        string No, string Title, int Sort, string[] RefCodes);

    // catalog ของหมายเหตุแบบตาราง breakdown (ตามชุด NOTE2 มาตรฐาน DBD / workbook JSPC)
    private static readonly NoteDef[] ScheduleDefs =
    [
        new("6.1",  "เงินสดและรายการเทียบเท่าเงินสด",        61, ["A1"]),
        new("6.2",  "ลูกหนี้การค้าและลูกหนี้หมุนเวียนอื่น",  62, ["A7", "A8"]),
        new("6.3",  "สินค้าคงเหลือ",                          63, ["A3"]),
        new("6.4",  "สินทรัพย์หมุนเวียนอื่น",                 64, ["A4", "TXR"]),
        new("6.8",  "สินทรัพย์ไม่หมุนเวียนอื่น",             68, ["A6"]),
        new("6.9",  "เจ้าหนี้การค้าและเจ้าหนี้หมุนเวียนอื่น", 69, ["L1", "L2", "TXP"]),
        new("6.10", "หนี้สินตามสัญญาเช่า",                    70, ["L4", "L5"]),
        new("6.14", "ค่าใช้จ่ายในการขาย",                     74, ["X1"]),
        new("6.15", "ค่าใช้จ่ายในการบริหาร",                  75, ["X2"]),
    ];

    /// <summary>
    /// แผนที่ RefCode (บรรทัดงบ) → เลขหมายเหตุ (NOTE2) สำหรับคอลัมน์ "หมายเหตุ" ในงบดุล/PL
    /// (รูปแบบ DBD). อ้างอิงตำแหน่งที่ตัวเลขปรากฏจริงในชุดหมายเหตุของระบบ:
    ///   schedule (6.1–6.4/6.8–6.10/6.14/6.15) + cost (6.13) + movement (6.6/6.7) + narrative (6.5/6.11).
    /// บรรทัดที่ไม่มีหมายเหตุ (A2, L3, C1, RE, I1–I4, X3, X4) = ไม่อยู่ใน map → คืน null.
    /// L2 (หนี้สินหมุนเวียนอื่น) จัดอยู่หมายเหตุ 6.9 ตามโครงสร้างหมายเหตุของระบบ.
    /// </summary>
    private static readonly Dictionary<string, string> RefCodeToNoteNo = new()
    {
        ["A1"] = "6.1",
        ["A7"] = "6.2", ["A8"] = "6.2",
        ["A3"] = "6.3",
        ["A4"] = "6.4", ["TXR"] = "6.4",
        ["A9"] = "6.5",
        ["A5"] = "6.6",
        ["A10"] = "6.7",
        ["A6"] = "6.8",
        ["L1"] = "6.9", ["L2"] = "6.9", ["TXP"] = "6.9",
        ["L4"] = "6.10", ["L5"] = "6.10",
        ["L6"] = "6.11",
        ["C"] = "6.13",
        ["X1"] = "6.14",
        ["X2"] = "6.15",
    };

    /// <summary>เลขหมายเหตุของบรรทัดงบ (null = บรรทัดนี้ไม่มีหมายเหตุประกอบ).</summary>
    public static string? NoteNoFor(string refCode)
        => RefCodeToNoteNo.GetValueOrDefault(refCode);

    /// <summary>RefCode ที่ใช้คิดสินค้าคงเหลือ (สำหรับต้นทุนขาย 6.13)</summary>
    private const string InventoryRef = "A3";

    /// <summary>RefCode ต้นทุนขาย (สำหรับ 6.13)</summary>
    private const string CostRef = "C";

    /// <summary>
    /// สร้างหมายเหตุแบบตาราง breakdown ทั้งหมด.
    /// </summary>
    /// <param name="allLines">บรรทัดงบมาตรฐาน (RefCode → section)</param>
    /// <param name="mappings">accountCode → mapping (RefCode)</param>
    /// <param name="accounts">accountCode → Account (ชื่อ)</param>
    /// <param name="current">ยอดสะสมสิ้นปีปัจจุบัน (accountCode → debit−credit)</param>
    /// <param name="prior">ยอดสะสมสิ้นปีก่อน</param>
    public static List<NoteScheduleDto> BuildSchedules(
        IReadOnlyList<StatementLine> allLines,
        Dictionary<string, AccountStatementMapping> mappings,
        Dictionary<string, Account> accounts,
        Dictionary<string, decimal> current,
        Dictionary<string, decimal> prior)
    {
        var sectionByRef = allLines.ToDictionary(l => l.RefCode, l => l.Section);
        var result = new List<NoteScheduleDto>();

        foreach (var def in ScheduleDefs)
        {
            var rows = BuildBreakdownRows(def.RefCodes, sectionByRef, mappings, accounts, current, prior);
            if (rows.Count == 0) continue;

            result.Add(new NoteScheduleDto(
                def.No, def.Title, def.Sort, rows,
                Math.Round(rows.Sum(r => r.CurrentYear), 2),
                Math.Round(rows.Sum(r => r.PriorYear), 2)));
        }

        return result;
    }

    /// <summary>
    /// ต้นทุนขายหรือต้นทุนการให้บริการ (6.13):
    /// องค์ประกอบต้นทุน = บัญชีกลุ่มต้นทุนขาย (RefCode "C") ยอดสะสมสิ้นปี (= ยอดต้นทุนขายในงบกำไรขาดทุน).
    /// ยอดรวม = ผลรวมองค์ประกอบ → "ลงตรง" กับบรรทัดต้นทุนขายในงบ PL เสมอ.
    ///
    /// สินค้าคงเหลือต้นงวด/ปลายงวด แสดงเป็น "ข้อมูลประกอบ" (memo) ไม่นำมาคิดในยอดรวม
    /// เพราะผังบัญชีลูกค้ารายนี้บันทึกต้นทุนขายแบบสุทธิ (perpetual) บัญชี "C" จึงเป็นต้นทุนขายสุทธิแล้ว
    /// ไม่ใช่ยอดซื้อ — การบวกต้นงวด/หักปลายงวดซ้ำจะทำให้ไม่ตรงงบ.
    /// </summary>
    public static NoteCostOfSalesDto? BuildCostOfSales(
        IReadOnlyList<StatementLine> allLines,
        Dictionary<string, AccountStatementMapping> mappings,
        Dictionary<string, Account> accounts,
        Dictionary<string, decimal> current,     // สะสมสิ้นปีปัจจุบัน
        Dictionary<string, decimal> prior,       // สะสมสิ้นปีก่อน (= ต้นงวดปีปัจจุบัน)
        Dictionary<string, decimal> twoPrior)    // สะสมสิ้นปีก่อนหน้านั้น (= ต้นงวดปีก่อน)
    {
        var sectionByRef = allLines.ToDictionary(l => l.RefCode, l => l.Section);

        var components = BuildBreakdownRows([CostRef], sectionByRef, mappings, accounts, current, prior);

        decimal openingCur = SumRef(InventoryRef, sectionByRef, mappings, prior);     // สิ้นปีก่อน
        decimal openingPri = SumRef(InventoryRef, sectionByRef, mappings, twoPrior);  // สิ้นปีก่อนหน้านั้น
        decimal closingCur = SumRef(InventoryRef, sectionByRef, mappings, current);
        decimal closingPri = SumRef(InventoryRef, sectionByRef, mappings, prior);

        // ถ้าไม่มีทั้งสินค้าคงเหลือและบัญชีต้นทุน → ไม่ออกหมายเหตุนี้
        if (components.Count == 0 && openingCur == 0 && closingCur == 0 && openingPri == 0 && closingPri == 0)
            return null;

        // ยอดรวม = ผลรวมองค์ประกอบต้นทุน (= บรรทัดต้นทุนขายในงบ PL)
        decimal totalCur = Math.Round(components.Sum(r => r.CurrentYear), 2);
        decimal totalPri = Math.Round(components.Sum(r => r.PriorYear), 2);

        return new NoteCostOfSalesDto(
            "6.13", "ต้นทุนขายหรือต้นทุนการให้บริการ", 73,
            openingCur, openingPri, components,
            closingCur, closingPri, totalCur, totalPri);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static List<NoteRowDto> BuildBreakdownRows(
        string[] refCodes,
        Dictionary<string, char> sectionByRef,
        Dictionary<string, AccountStatementMapping> mappings,
        Dictionary<string, Account> accounts,
        Dictionary<string, decimal> current,
        Dictionary<string, decimal> prior)
    {
        var refSet = new HashSet<string>(refCodes);

        // บัญชีทั้งหมดที่ map เข้า refCodes เหล่านี้ (เรียงตามรหัสบัญชี)
        var accountCodes = mappings
            .Where(kv => refSet.Contains(kv.Value.RefCode))
            .Select(kv => kv.Key)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var rows = new List<NoteRowDto>();
        foreach (var code in accountCodes)
        {
            var refCode = mappings[code].RefCode;
            char section = sectionByRef.TryGetValue(refCode, out var s) ? s : 'A';

            decimal cur = Present(section, current.GetValueOrDefault(code));
            decimal pri = Present(section, prior.GetValueOrDefault(code));
            if (cur == 0m && pri == 0m) continue;   // ข้ามบัญชีที่ไม่มียอดทั้งสองปี

            var name = accounts.TryGetValue(code, out var acc) ? acc.AccountName : mappings[code].AccountName;
            rows.Add(new NoteRowDto(name, Math.Round(cur, 2), Math.Round(pri, 2)));
        }

        return rows;
    }

    private static decimal SumRef(
        string refCode,
        Dictionary<string, char> sectionByRef,
        Dictionary<string, AccountStatementMapping> mappings,
        Dictionary<string, decimal> nets)
    {
        char section = sectionByRef.TryGetValue(refCode, out var s) ? s : 'A';
        decimal sum = 0m;
        foreach (var (code, mapping) in mappings)
        {
            if (mapping.RefCode != refCode) continue;
            sum += Present(section, nets.GetValueOrDefault(code));
        }
        return Math.Round(sum, 2);
    }

    /// <summary>sign convention: A/X คงเครื่องหมาย, L/E/I กลับเครื่องหมาย.</summary>
    private static decimal Present(char section, decimal net)
        => section is 'A' or 'X' ? net : -net;

    // ── การแทน placeholder ในข้อความ template ────────────────────────────────

    public static string Substitute(string body, NotesPlaceholders p)
        => body
            .Replace("{{CompanyName}}", p.CompanyName)
            .Replace("{{TaxId}}", p.TaxId)
            .Replace("{{Address}}", p.Address)
            .Replace("{{FiscalYear}}", p.FiscalYear.ToString())
            .Replace("{{FiscalYearTh}}", p.FiscalYearTh.ToString())
            .Replace("{{PriorYear}}", p.PriorYear.ToString())
            .Replace("{{PriorYearTh}}", p.PriorYearTh.ToString());
}

public readonly record struct NotesPlaceholders(
    string CompanyName,
    string TaxId,
    string Address,
    int FiscalYear,
    int FiscalYearTh,
    int PriorYear,
    int PriorYearTh);
