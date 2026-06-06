using System.Globalization;
using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Payroll.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Datacenter.Infrastructure.Services.Payroll;

/// <summary>
/// แบบ กท.20ก — แบบแสดงเงินค่าจ้างประจำปี กองทุนเงินทดแทน.
/// PDF = ฟอร์มสรุป (1 บรรทัด: ค่าจ้างรวม/ลูกจ้าง/อัตรา/เงินสมทบ) + ใบแนบรายคน.
/// Excel = รายชื่อลูกจ้าง+ค่าจ้างทั้งปี+ค่าจ้างที่ใช้คำนวณ (สำหรับ e-Wage).
/// </summary>
public class Kt20ExportService(string fontFamily) : IKt20ExportService
{
    private readonly string _font = string.IsNullOrWhiteSpace(fontFamily) ? "Tahoma" : fontFamily;
    private static string Money(decimal v) => v.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static readonly string[] Headers =
        ["ลำดับ", "เลขประจำตัวประชาชน", "คำนำหน้า", "ชื่อ", "สกุล", "ค่าจ้างทั้งปี", "ค่าจ้างที่ใช้คำนวณ"];

    public byte[] BuildExcel(Kt20Dto d)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"กท20ก-{d.Year + 543}");
        ws.Cell(1, 1).Value =
            $"แบบแสดงเงินค่าจ้างประจำปี (กท.20ก) ประจำปี {d.Year + 543} — {d.CompanyName} " +
            $"(เลขที่บัญชีกองทุนเงินทดแทน {d.WcfAccountNo} {d.WcfBranchCode})";
        ws.Range(1, 1, 1, Headers.Length).Merge().Style.Font.SetBold().Font.FontSize = 13;
        for (int c = 0; c < Headers.Length; c++)
        {
            var cell = ws.Cell(2, c + 1);
            cell.Value = Headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        int r = 3;
        foreach (var row in d.Rows)
        {
            ws.Cell(r, 1).Value = row.Seq;
            ws.Cell(r, 2).Value = row.NationalId; ws.Cell(r, 2).Style.NumberFormat.Format = "@";
            ws.Cell(r, 3).Value = row.Prefix;
            ws.Cell(r, 4).Value = row.FirstName;
            ws.Cell(r, 5).Value = row.LastName;
            ws.Cell(r, 6).Value = row.AnnualWage;
            ws.Cell(r, 7).Value = row.CappedWage;
            r++;
        }
        ws.Cell(r, 5).Value = $"รวม {d.EmployeeCount} คน"; ws.Cell(r, 5).Style.Font.Bold = true;
        ws.Cell(r, 7).Value = d.TotalWage; ws.Cell(r, 7).Style.Font.Bold = true;
        ws.Cell(r + 1, 5).Value = $"อัตรา {d.RatePct:0.##}% → เงินสมทบ";
        ws.Cell(r + 1, 7).Value = d.Contribution; ws.Cell(r + 1, 7).Style.Font.Bold = true;
        ws.Column(2).Width = 20; ws.Column(4).Width = 16; ws.Column(5).Width = 16;
        ws.Columns(6, 7).Width = 16;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] BuildPdf(Kt20Dto d) => BuildDocument(d).GeneratePdf();

    public IReadOnlyList<byte[]> BuildImages(Kt20Dto d) =>
        BuildDocument(d)
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 150 })
            .ToList();

    private Document BuildDocument(Kt20Dto d) =>
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily(_font).FontSize(11).LineHeight(1.2f));
                page.Content().Element(e => Compose(e, d));
            });
        });

    private void Compose(IContainer container, Kt20Dto d)
    {
        container.Column(col =>
        {
            // ── หัวเรื่อง: ชื่อแบบกึ่งกลาง + "กท. 20 ก" ชิดขวา (สมดุลด้วย ConstantItem ซ้าย) ──
            col.Item().Row(r =>
            {
                r.ConstantItem(70);
                r.RelativeItem().AlignCenter().Text("แบบแสดงเงินค่าจ้างประจำปีกองทุนเงินทดแทน").Bold().FontSize(14);
                r.ConstantItem(70).AlignRight().Text("กท. 20 ก").Bold().FontSize(12);
            });

            // ── ชื่อนายจ้าง | เลขที่บัญชี ──
            col.Item().PaddingTop(6).Row(r =>
            {
                r.RelativeItem(2).Text(t => { t.Span("ชื่อ  ").Bold(); t.Span(d.CompanyName); });
                r.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("เลขที่บัญชี  ").Bold();
                    t.Span($"{d.WcfAccountNo} {d.WcfBranchCode}");
                });
            });
            if (!string.IsNullOrWhiteSpace(d.Address))
                col.Item().Text($"ที่อยู่  {d.Address}").FontSize(9);

            // ── ตารางสรุปตามแบบราชการ ──
            col.Item().PaddingTop(8).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(70);   // ประจำปี
                    c.ConstantColumn(46);   // รหัส
                    c.ConstantColumn(46);   // ลูกจ้าง
                    c.RelativeColumn();     // ประเภทกิจการ
                    c.ConstantColumn(96);   // เงินค่าจ้าง
                    c.ConstantColumn(56);   // อัตรา
                    c.ConstantColumn(86);   // เงินสมทบ
                });
                IContainer H(IContainer x) => x.Border(0.6f).Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3);
                t.Header(h =>
                {
                    h.Cell().Element(H).AlignCenter().Text("ประจำปี").FontSize(9).Bold();
                    h.Cell().Element(H).AlignCenter().Text("รหัส").FontSize(9).Bold();
                    h.Cell().Element(H).AlignCenter().Text("ลูกจ้าง").FontSize(9).Bold();
                    h.Cell().Element(H).AlignCenter().Text("ประเภทกิจการ").FontSize(9).Bold();
                    h.Cell().Element(H).AlignCenter().Text("เงินค่าจ้าง").FontSize(9).Bold();
                    h.Cell().Element(H).AlignCenter().Text("อัตราเงินสมทบ\nร้อยละ").FontSize(8).Bold();
                    h.Cell().Element(H).AlignCenter().Text("เงินสมทบ").FontSize(9).Bold();
                });
                IContainer C(IContainer x) => x.Border(0.6f).PaddingVertical(6).PaddingHorizontal(3);
                t.Cell().Element(C).AlignCenter().Text($"ม.ค.-ธ.ค.{(d.Year + 543) % 100:00}").FontSize(9);
                t.Cell().Element(C).Text("");                                  // รหัสประเภทกิจการ (กรอกเอง)
                t.Cell().Element(C).AlignCenter().Text(d.EmployeeCount.ToString()).FontSize(9);
                t.Cell().Element(C).Text("");                                  // ประเภทกิจการ (กรอกเอง)
                t.Cell().Element(C).AlignRight().Text(Money(d.TotalWage)).FontSize(9);
                t.Cell().Element(C).AlignCenter().Text($"{d.RatePct:0.##}").FontSize(9);
                t.Cell().Element(C).AlignRight().Text(Money(d.Contribution)).FontSize(9);
            });

            col.Item().PaddingTop(3).Text(t =>
            {
                t.Span("จำนวนเงินสมทบ (ตัวอักษร)  ").Bold();
                t.Span($"({d.ContributionText})");
            });

            // ── รับรอง + ลงชื่อ ──
            col.Item().PaddingTop(10).Text("ข้าพเจ้าขอรับรองว่าข้อความที่แจ้งนี้เป็นความจริงทุกประการ").FontSize(10);
            col.Item().PaddingTop(14).Row(r =>
            {
                r.RelativeItem();
                r.ConstantItem(280).Column(s =>
                {
                    s.Item().AlignCenter().Text("ลงชื่อ ......................................................").FontSize(10);
                    s.Item().AlignCenter().Text("ผู้มีอำนาจลงนาม (ประทับตรา)").FontSize(9);
                });
            });

            // ── หมายเหตุกฎเกณฑ์ ──
            col.Item().PaddingTop(10).Text(
                $"หมายเหตุ: เงินค่าจ้างสูงสุดคนละไม่เกิน {Money(d.WageCapPerYear)} บาท/ปี · " +
                "เงินค่าจ้าง = ค่าตอบแทนการทำงานในเวลาปกติ ไม่รวมค่าล่วงเวลา ค่าทำงานในวันหยุด และโบนัส · " +
                "จำนวนลูกจ้าง = ผู้มีค่าจ้าง ณ วันที่ 31 ธันวาคม")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);

            // ── ใบแนบ: รายละเอียดค่าจ้างรายคน ──
            col.Item().PaddingTop(16).Text($"ใบแนบ — รายละเอียดค่าจ้างรายคน ประจำปี {d.Year + 543}").Bold().FontSize(11);
            col.Item().PaddingTop(4).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(32);   // ลำดับ
                    c.ConstantColumn(110);  // เลขบัตร
                    c.RelativeColumn();     // ชื่อ-สกุล
                    c.ConstantColumn(96);   // ค่าจ้างทั้งปี
                    c.ConstantColumn(100);  // ค่าจ้างที่ใช้คำนวณ
                });
                IContainer H(IContainer x) => x.BorderBottom(1).PaddingVertical(4).PaddingHorizontal(4);
                t.Header(h =>
                {
                    h.Cell().Element(H).AlignCenter().Text("ลำดับ").Bold().FontSize(9);
                    h.Cell().Element(H).Text("เลขประจำตัวประชาชน").Bold().FontSize(9);
                    h.Cell().Element(H).Text("ชื่อ-สกุล").Bold().FontSize(9);
                    h.Cell().Element(H).AlignRight().Text("ค่าจ้างทั้งปี").Bold().FontSize(9);
                    h.Cell().Element(H).AlignRight().Text("ค่าจ้างที่ใช้คำนวณ").Bold().FontSize(9);
                });
                foreach (var row in d.Rows)
                {
                    IContainer Cc(IContainer x) => x.BorderBottom(0.4f).PaddingVertical(2.5f).PaddingHorizontal(4);
                    t.Cell().Element(Cc).AlignCenter().Text(row.Seq.ToString()).FontSize(9);
                    t.Cell().Element(Cc).Text(row.NationalId).FontSize(9);
                    t.Cell().Element(Cc).Text($"{row.Prefix}{row.FirstName} {row.LastName}").FontSize(9);
                    t.Cell().Element(Cc).AlignRight().Text(Money(row.AnnualWage)).FontSize(9);
                    t.Cell().Element(Cc).AlignRight().Text(Money(row.CappedWage)).FontSize(9);
                }
                t.Cell().ColumnSpan(3).BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text($"รวม {d.EmployeeCount} คน").Bold().FontSize(9);
                t.Cell().BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Money(d.Rows.Sum(x => x.AnnualWage))).Bold().FontSize(9);
                t.Cell().BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Money(d.TotalWage)).Bold().FontSize(9);
            });
        });
    }
}
