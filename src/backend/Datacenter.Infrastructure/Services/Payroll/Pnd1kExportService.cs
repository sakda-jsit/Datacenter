using System.Globalization;
using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Payroll.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Datacenter.Infrastructure.Services.Payroll;

/// <summary>ใบแนบ ภ.ง.ด.1ก — Excel (รายชื่อ+เงินได้+ภาษีทั้งปี) + PDF</summary>
public class Pnd1kExportService(string fontFamily) : IPnd1kExportService
{
    private readonly string _font = string.IsNullOrWhiteSpace(fontFamily) ? "Tahoma" : fontFamily;
    private static string Money(decimal v) => v.ToString("#,##0.00", CultureInfo.InvariantCulture);

    private static readonly string[] Headers =
        ["ลำดับ", "เลขประจำตัวผู้เสียภาษี", "คำนำหน้า", "ชื่อ", "สกุล", "ประเภทเงินได้", "เงินได้ทั้งปี", "ภาษีที่หัก", "เงื่อนไข"];

    public byte[] BuildExcel(Pnd1kDto d)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"ภงด1ก-{d.Year + 543}");
        ws.Cell(1, 1).Value = $"ใบแนบ ภ.ง.ด.1ก ประจำปี {d.Year + 543} — {d.CompanyName} (เลขผู้เสียภาษี {d.TaxId})";
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
            ws.Cell(r, 6).Value = row.IncomeTypeCode;
            ws.Cell(r, 7).Value = row.AnnualIncome;
            ws.Cell(r, 8).Value = row.AnnualTax;
            ws.Cell(r, 9).Value = row.Condition;
            r++;
        }
        ws.Cell(r, 6).Value = "รวม"; ws.Cell(r, 6).Style.Font.Bold = true;
        ws.Cell(r, 7).Value = d.TotalIncome; ws.Cell(r, 7).Style.Font.Bold = true;
        ws.Cell(r, 8).Value = d.TotalTax; ws.Cell(r, 8).Style.Font.Bold = true;
        ws.Column(2).Width = 18; ws.Column(4).Width = 16; ws.Column(5).Width = 16;
        ws.Columns(7, 8).Width = 14;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] BuildPdf(Pnd1kDto d)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily(_font).FontSize(11).LineHeight(1.2f));
                page.Content().Element(e => Compose(e, d));
            });
        }).GeneratePdf();
    }

    private void Compose(IContainer container, Pnd1kDto d)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text("ใบแนบ ภ.ง.ด.1ก").FontSize(15).Bold();
            col.Item().PaddingBottom(2).AlignCenter().Text($"สรุปการจ่ายเงินได้และภาษีที่หักนำส่ง ประจำปี {d.Year + 543}").FontSize(10);
            col.Item().Text(t => { t.Span("ผู้มีหน้าที่หักภาษี ณ ที่จ่าย  ").Bold(); t.Span(d.CompanyName); });
            col.Item().PaddingBottom(6).Text($"เลขประจำตัวผู้เสียภาษี  {d.TaxId}");

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(32);   // ลำดับ
                    c.ConstantColumn(110);  // เลขผู้เสียภาษี
                    c.RelativeColumn();     // ชื่อ-สกุล
                    c.ConstantColumn(60);   // ประเภท
                    c.ConstantColumn(90);   // เงินได้
                    c.ConstantColumn(80);   // ภาษี
                });
                IContainer H(IContainer x) => x.BorderBottom(1).PaddingVertical(4).PaddingHorizontal(4);
                t.Header(h =>
                {
                    h.Cell().Element(H).AlignCenter().Text("ลำดับ").Bold();
                    h.Cell().Element(H).Text("เลขประจำตัวผู้เสียภาษี").Bold();
                    h.Cell().Element(H).Text("ชื่อ-สกุล").Bold();
                    h.Cell().Element(H).AlignCenter().Text("ประเภท").Bold();
                    h.Cell().Element(H).AlignRight().Text("เงินได้ทั้งปี").Bold();
                    h.Cell().Element(H).AlignRight().Text("ภาษีที่หัก").Bold();
                });
                foreach (var row in d.Rows)
                {
                    IContainer C(IContainer x) => x.BorderBottom(0.4f).PaddingVertical(2.5f).PaddingHorizontal(4);
                    t.Cell().Element(C).AlignCenter().Text(row.Seq.ToString());
                    t.Cell().Element(C).Text(row.NationalId);
                    t.Cell().Element(C).Text($"{row.Prefix}{row.FirstName} {row.LastName}");
                    t.Cell().Element(C).AlignCenter().Text(row.IncomeTypeCode);
                    t.Cell().Element(C).AlignRight().Text(Money(row.AnnualIncome));
                    t.Cell().Element(C).AlignRight().Text(Money(row.AnnualTax));
                }
                t.Cell().ColumnSpan(4).BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text($"รวม {d.PersonCount} ราย").Bold();
                t.Cell().BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Money(d.TotalIncome)).Bold();
                t.Cell().BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Money(d.TotalTax)).Bold();
            });
            col.Item().PaddingTop(8).Text("ประเภทเงินได้ 40(1) = เงินเดือน/ค่าจ้าง · เงื่อนไข 1 = หักภาษี ณ ที่จ่าย").FontSize(8).Italic();
        });
    }
}
