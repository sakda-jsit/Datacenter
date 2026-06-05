using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.DTOs;

namespace Datacenter.Infrastructure.Services.Notes;

/// <summary>
/// สร้าง Excel หมายเหตุประกอบงบการเงิน (NOTE2) ให้เหมือน sheet NOTE2 ของ workbook อ้างอิง:
/// ฟอนต์ AngsanaUPC (หัว 18 bold / เนื้อ 16), หัวเอกสาร+ลงชื่อกรรมการทุกหน้า,
/// คอลัมน์ปีปัจจุบัน(F)/ปีก่อน(H), ตัวเลขแบบบัญชี, ตารางการเคลื่อนไหวมีกรอบ,
/// แบ่งหน้า A4 ตามกลุ่มหมายเหตุ (เหมือนงบที่ยื่น).
/// </summary>
public class Note2ExcelExporter : INote2ExcelExporter
{
    private const string Font = "AngsanaUPC";
    private const string Acc = "_-* #,##0.00_-;\\-* #,##0.00_-;_-* \"-\"??_-;_-@_-";
    // คอลัมน์ (1-based): A เลขข้อ, B label, C-D ข้อความ/ราคาทุน, E เพิ่ม, F ลด/ปี2568, G คั่น, H ปี2567/ปลายปี
    private const int A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8;

    // ขึ้นหน้าใหม่หลังหมายเหตุเหล่านี้ (ตามหน้างบอ้างอิง)
    private static readonly HashSet<string> PageEndAfter =
        ["2", "3", "5", "6.3", "6.5", "6.6", "6.8", "6.10", "6.13", "6.15", "7"];

    public byte[] Build(NotesToFsDto data, string directorName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("NOTE2");
        ws.Style.Font.FontName = Font;
        ws.Style.Font.FontSize = 16;
        ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Column(A).Width = 4.1; ws.Column(B).Width = 4.6; ws.Column(C).Width = 16.6;
        ws.Column(D).Width = 16.0; ws.Column(E).Width = 12.0; ws.Column(F).Width = 13.0;
        ws.Column(G).Width = 2.4; ws.Column(H).Width = 15.4;

        ws.PageSetup.PageOrientation = XLPageOrientation.Portrait;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.Margins.Left = 0.5; ws.PageSetup.Margins.Right = 0.2;
        ws.PageSetup.Margins.Top = 0.5; ws.PageSetup.Margins.Bottom = 0.3;
        ws.PageSetup.CenterHorizontally = true;
        // ใช้สเกลคงที่ (ไม่ใช่ "Fit to") เพื่อให้ Page Break Preview ลาก/ปรับเส้นแบ่งหน้าได้
        // (โหมด Fit-to-page จะล็อก preview ให้ย่อสเกลแทนการลากเส้น)
        ws.PageSetup.Scale = 90;

        int yTh = data.FiscalYear + 543, pTh = data.PriorYear + 543;
        var items = Order(data);

        var ctx = new Ctx(ws, data, directorName, yTh, pTh);
        ctx.Header();

        for (int i = 0; i < items.Count; i++)
        {
            var (no, render) = items[i];
            render(ctx);
            bool last = i == items.Count - 1;
            if (PageEndAfter.Contains(no) || last)
            {
                ctx.Signature();
                if (!last) { ws.PageSetup.AddHorizontalPageBreak(ctx.Row - 1); ctx.Header(); }
            }
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // เรียงหมายเหตุทั้งหมดตาม SortOrder + ผูก renderer
    private static List<(string No, Action<Ctx> Render)> Order(NotesToFsDto d)
    {
        var list = new List<(int Sort, string No, Action<Ctx>)>();
        foreach (var n in d.Narratives) { var x = n; list.Add((x.SortOrder, x.NoteNo, c => c.Narrative(x))); }
        foreach (var s in d.Schedules) { var x = s; list.Add((x.SortOrder, x.NoteNo, c => c.Schedule(x))); }
        foreach (var m in d.Movements) { var x = m; list.Add((x.SortOrder, x.NoteNo, c => c.Movement(x))); }
        if (d.CostOfSales is { } cos) list.Add((cos.SortOrder, cos.NoteNo, c => c.CostOfSales(cos)));
        return list.OrderBy(x => x.Sort).Select(x => (x.No, x.Item3)).ToList();
    }

    // ── context ที่ถือ worksheet + ตำแหน่งแถวปัจจุบัน ────────────────────────────
    private sealed class Ctx(IXLWorksheet ws, NotesToFsDto data, string director, int yTh, int pTh)
    {
        public int Row = 1;
        private readonly IXLWorksheet _ws = ws;

        private IXLCell Cell(int r, int c) => _ws.Cell(r, c);

        private void MergeCenter(int r, int c1, int c2, string text, int size, bool bold)
        {
            var cell = Cell(r, c1);
            cell.Value = text;
            cell.Style.Font.FontSize = size; cell.Style.Font.Bold = bold;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            _ws.Range(r, c1, r, c2).Merge();
        }

        public void Header()
        {
            MergeCenter(Row++, A, H, data.ClientName, 18, true);
            MergeCenter(Row++, A, H, "หมายเหตุประกอบงบการเงิน", 18, true);
            MergeCenter(Row++, A, H, data.PeriodLabel, 18, true);
        }

        public void Signature()
        {
            Row++; // เว้นบรรทัด
            MergeCenter(Row++, A, H, "ลงชื่อ ........................................................... กรรมการ", 16, false);
            MergeCenter(Row++, A, H, $"( {(string.IsNullOrWhiteSpace(director) ? "..........................................." : director)} )", 16, false);
        }

        private void Text(int col, string text, bool bold = false)
        {
            var cell = Cell(Row, col);
            cell.Value = text; cell.Style.Font.Bold = bold;
        }

        private void Amount(int col, decimal v, bool bold, XLBorderStyleValues top = XLBorderStyleValues.None, XLBorderStyleValues bottom = XLBorderStyleValues.None)
        {
            var cell = Cell(Row, col);
            cell.Value = (double)v;
            cell.Style.NumberFormat.Format = Acc;
            cell.Style.Font.Bold = bold;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            if (top != XLBorderStyleValues.None) cell.Style.Border.TopBorder = top;
            if (bottom != XLBorderStyleValues.None) cell.Style.Border.BottomBorder = bottom;
        }

        // ── หัวข้อหมายเหตุ + แถวหน่วย + แถวปี ──
        private void NoteTitle(string no, string title)
        {
            Text(A, no, true); Text(B, title, true);
            Row++;
        }
        private void UnitRow()
        {
            var cell = Cell(Row, H); cell.Value = "หน่วย:บาท";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            Row++;
        }
        private void YearHeader()
        {
            foreach (var (col, txt) in new[] { (F, yTh.ToString()), (H, pTh.ToString()) })
            {
                var cell = Cell(Row, col); cell.Value = txt;
                cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            Row++;
        }

        public void Narrative(NoteNarrativeDto n)
        {
            NoteTitle($"{n.NoteNo}.", n.Title);
            foreach (var p in n.Body.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(p)) { Row++; continue; }
                var cell = Cell(Row, B);
                cell.Value = p;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                _ws.Range(Row, B, Row, H).Merge();
                int lines = Math.Max(1, (int)Math.Ceiling(p.Length / 88.0));
                _ws.Row(Row).Height = lines * 21;
                Row++;
            }
            // ตารางอัตราค่าเสื่อม (เฉพาะข้อ 3 — แนบหลังนโยบาย ที่ดิน อาคาร อุปกรณ์)
            if (n.NoteNo == "3") DepreciationRateTable();
            Row++;
        }

        private void DepreciationRateTable()
        {
            var rates = new (string Name, int Years)[]
            {
                ("อาคารและส่วนปรับปรุงอาคาร", 20), ("เครื่องจักร", 5), ("เครื่องมือเครื่องใช้", 5),
                ("อุปกรณ์สำนักงาน", 5), ("เครื่องตกแต่งสำนักงาน", 5), ("คอมพิวเตอร์", 3),
                ("โปรแกรมคอมพิวเตอร์", 3), ("ยานพาหนะ", 5),
            };
            Row++;
            Text(C, "อัตราการคิดค่าเสื่อมราคา (ปี)", true); Row++;
            foreach (var (name, years) in rates)
            {
                Cell(Row, C).Value = name;
                var yc = Cell(Row, F); yc.Value = years; yc.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                var pc = Cell(Row, H); pc.Value = "ปี"; pc.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                Row++;
            }
        }

        public void Schedule(NoteScheduleDto s)
        {
            NoteTitle(s.NoteNo, s.Title);
            UnitRow();
            YearHeader();
            foreach (var r in s.Rows)
            {
                Text(B, r.Label);
                Amount(F, r.CurrentYear, false);
                Amount(H, r.PriorYear, false);
                Row++;
            }
            Text(B, "รวม", true);
            Amount(F, s.TotalCurrent, true, XLBorderStyleValues.Thin, XLBorderStyleValues.Double);
            Amount(H, s.TotalPrior, true, XLBorderStyleValues.Thin, XLBorderStyleValues.Double);
            Row++;
        }

        public void CostOfSales(NoteCostOfSalesDto cos)
        {
            NoteTitle(cos.NoteNo, cos.Title);
            UnitRow();
            YearHeader();
            Text(B, "สินค้าคงเหลือต้นงวด");
            Amount(F, cos.OpeningInventoryCurrent, false); Amount(H, cos.OpeningInventoryPrior, false); Row++;
            foreach (var r in cos.Components)
            {
                Text(B, "บวก  " + r.Label);
                Amount(F, r.CurrentYear, false); Amount(H, r.PriorYear, false); Row++;
            }
            Text(B, "หัก  สินค้าคงเหลือปลายงวด");
            Amount(F, cos.ClosingInventoryCurrent, false); Amount(H, cos.ClosingInventoryPrior, false); Row++;
            Text(B, "รวมต้นทุน", true);
            Amount(F, cos.TotalCurrent, true, XLBorderStyleValues.Thin, XLBorderStyleValues.Double);
            Amount(H, cos.TotalPrior, true, XLBorderStyleValues.Thin, XLBorderStyleValues.Double);
            Row++;
        }

        public void Movement(NoteMovementDto m)
        {
            NoteTitle(m.NoteNo, m.Title);
            UnitRow();
            int topRow = Row;
            // หัวคอลัมน์ (2 แถว)
            void Boxed(int col, string txt, bool merge2 = false)
            {
                var cell = Cell(Row, col); cell.Value = txt; cell.Style.Font.Bold = true;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.WrapText = true;
            }
            Boxed(D, "ยอดคงเหลือ"); Boxed(E, "รายการเคลื่อนไหวระหว่างปี"); _ws.Range(Row, E, Row, F).Merge(); Boxed(H, "ยอดคงเหลือ");
            Row++;
            Boxed(D, $"31 ธันวาคม {pTh}"); Boxed(E, "เพิ่มขึ้น"); Boxed(F, "ลดลง"); Boxed(H, $"31 ธันวาคม {yTh}");
            Row++;

            Text(B, "ราคาทุน", true); Row++;
            foreach (var r in m.CostRows) MovRow(r, false);
            MovRow(m.CostTotal, true);
            Text(B, "หักค่าเสื่อมราคาสะสม", true); Row++;
            foreach (var r in m.AccumRows) MovRow(r, false);
            MovRow(m.AccumTotal, true);

            // สุทธิ + ค่าเสื่อม/ตัดจำหน่ายสำหรับปี (D = ปีก่อน, H = ปีปัจจุบัน)
            Text(B, $"{m.Title}-สุทธิ", true);
            Amount(D, m.NetOpening, true); Amount(H, m.NetClosing, true); Row++;
            string chargeLabel = m.NoteNo == "6.7" ? "ค่าตัดจำหน่ายสำหรับปี" : "ค่าเสื่อมราคาสำหรับปี";
            Text(B, chargeLabel, true);
            Amount(D, m.ChargeForYearPrior, true); Amount(H, m.ChargeForYear, true);
            int bottomRow = Row;
            Row++;

            // กรอบรอบตาราง: คอลัมน์ D,E,F และ H ตั้งแต่หัวถึงแถวสุดท้าย
            var box = _ws.Range(topRow, D, bottomRow, F);
            box.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            box.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            box.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            var hbox = _ws.Range(topRow, H, bottomRow, H);
            hbox.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            // เส้นใต้หัวคอลัมน์
            _ws.Range(topRow + 1, D, topRow + 1, F).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            _ws.Cell(topRow + 1, H).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            Row++;
        }

        private void MovRow(NoteMovementRowDto r, bool bold)
        {
            Text(B, r.Label, bold);
            var t = bold ? XLBorderStyleValues.Thin : XLBorderStyleValues.None;
            var bt = bold ? XLBorderStyleValues.Thin : XLBorderStyleValues.None;
            Amount(D, r.Opening, bold, t, bt);
            Amount(E, r.Additions, bold, t, bt);
            Amount(F, r.Disposals, bold, t, bt);
            Amount(H, r.Closing, bold, t, bt);
            Row++;
        }
    }
}
