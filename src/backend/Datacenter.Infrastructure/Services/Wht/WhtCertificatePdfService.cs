using System.Globalization;
using Datacenter.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Datacenter.Infrastructure.Services.Wht;

/// <summary>
/// สร้าง PDF หนังสือรับรองการหักภาษี ณ ที่จ่าย (50 ทวิ) ด้วย QuestPDF — 1 ใบ/หน้า.
/// ใช้ฟอนต์ไทยจากระบบ (ค่าเริ่มต้น Tahoma; ตั้งได้ที่ config "Wht:CertificateFont").
/// </summary>
public class WhtCertificatePdfService(string fontFamily) : IWhtCertificatePdfService
{
    private readonly string _font = string.IsNullOrWhiteSpace(fontFamily) ? "Tahoma" : fontFamily;

    public byte[] Generate(IReadOnlyList<WhtCertificateModel> certificates)
    {
        var doc = Document.Create(container =>
        {
            foreach (var c in certificates)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.4f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily(_font).FontSize(11).LineHeight(1.25f));
                    page.Content().Element(e => Compose(e, c));
                });
            }
        });

        return doc.GeneratePdf();
    }

    private void Compose(IContainer container, WhtCertificateModel c)
    {
        container.Column(col =>
        {
            // หัวเรื่อง + มุมขวา
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(t =>
                {
                    t.Item().AlignCenter().Text("หนังสือรับรองการหักภาษี ณ ที่จ่าย").Bold().FontSize(15);
                    t.Item().AlignCenter().Text("ตามมาตรา 50 ทวิ แห่งประมวลรัษฎากร").FontSize(11);
                });
            });

            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(txt =>
                {
                    txt.Span("ฉบับที่ 1 ").Bold();
                    txt.Span("สำหรับผู้ถูกหักภาษี ณ ที่จ่าย ใช้แนบพร้อมกับแบบแสดงรายการภาษี");
                });
                row.ConstantItem(170).AlignRight().Text($"ในแบบยื่น {c.FormLabel}  เลขที่ {c.SequenceNo}");
            });

            // ผู้มีหน้าที่หักภาษี
            col.Item().PaddingTop(8).Element(e => PartyBlock(e,
                "ผู้มีหน้าที่หักภาษี ณ ที่จ่าย", c.PayerTaxId, c.PayerName, c.PayerAddress));

            // ผู้ถูกหักภาษี
            col.Item().PaddingTop(6).Element(e => PartyBlock(e,
                "ผู้ถูกหักภาษี ณ ที่จ่าย", c.PayeeTaxId, c.PayeeName, c.PayeeAddress));

            // ตารางเงินได้
            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(5);
                    cd.RelativeColumn(2);
                    cd.RelativeColumn(2);
                    cd.RelativeColumn(2);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("ประเภทเงินได้พึงประเมินที่จ่าย");
                    h.Cell().Element(HeadCell).AlignCenter().Text("วัน เดือน ปี ที่จ่าย");
                    h.Cell().Element(HeadCell).AlignRight().Text("จำนวนเงินที่จ่าย");
                    h.Cell().Element(HeadCell).AlignRight().Text("ภาษีที่หักและนำส่งไว้");
                });

                table.Cell().Element(BodyCell).Text(c.IncomeType);
                table.Cell().Element(BodyCell).AlignCenter().Text(FormatThaiDate(c.PayDate));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(c.Amount));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(c.TaxAmount));

                // รวม
                table.Cell().Element(TotalCell).Text("รวมเงินที่จ่ายและภาษีที่หักนำส่ง").Bold();
                table.Cell().Element(TotalCell).Text("");
                table.Cell().Element(TotalCell).AlignRight().Text(Money(c.Amount)).Bold();
                table.Cell().Element(TotalCell).AlignRight().Text(Money(c.TaxAmount)).Bold();
            });

            col.Item().PaddingTop(6).Text(txt =>
            {
                txt.Span("รวมเงินภาษีที่หักนำส่ง (ตัวอักษร)  ").Bold();
                txt.Span($"({c.AmountInWords})");
            });

            // ผู้จ่ายเงิน
            col.Item().PaddingTop(8).Text("ผู้จ่ายเงิน   ☑ หักภาษี ณ ที่จ่าย   ☐ ออกภาษีให้ตลอดไป   ☐ ออกภาษีให้ครั้งเดียว");

            col.Item().PaddingTop(10).Text("ขอรับรองว่าข้อความและตัวเลขดังกล่าวข้างต้นถูกต้องตรงกับความเป็นจริงทุกประการ").FontSize(10);

            col.Item().PaddingTop(18).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(240).Column(s =>
                {
                    s.Item().AlignCenter().Text("ลงชื่อ ......................................................... ผู้จ่ายเงิน");
                    s.Item().AlignCenter().PaddingTop(4).Text($"วันเดือนปีที่ออกหนังสือรับรองฯ  {FormatThaiDate(c.IssueDate)}");
                });
            });
        });
    }

    private static void PartyBlock(IContainer container, string title, string taxId, string name, string? address)
    {
        container.Column(col =>
        {
            col.Item().Text(txt =>
            {
                txt.Span($"{title}: ").Bold();
                txt.Span($"เลขประจำตัวผู้เสียภาษี {taxId}");
            });
            col.Item().Text($"ชื่อ  {name}");
            if (!string.IsNullOrWhiteSpace(address))
                col.Item().Text($"ที่อยู่  {address}").FontSize(10);
        });
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Border(0.8f).Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).DefaultTextStyle(t => t.SemiBold());

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.8f).PaddingVertical(6).PaddingHorizontal(4).MinHeight(28);

    private static IContainer TotalCell(IContainer c) =>
        c.Border(0.8f).Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(4);

    private static string Money(decimal v) =>
        v.ToString("#,##0.00", CultureInfo.InvariantCulture);

    /// <summary>dd/MM/ปีพ.ศ.(2 หลัก) เช่น 01/04/69</summary>
    private static string FormatThaiDate(DateTime? d)
    {
        if (d is null) return "";
        int beYear = d.Value.Year + 543;
        return $"{d.Value.Day:00}/{d.Value.Month:00}/{beYear % 100:00}";
    }
}
