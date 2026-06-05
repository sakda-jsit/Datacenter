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
        => BuildDocument(certificates).GeneratePdf();

    public IReadOnlyList<byte[]> GenerateImages(IReadOnlyList<WhtCertificateModel> certificates)
        => BuildDocument(certificates)
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 150 })
            .ToList();

    private Document BuildDocument(IReadOnlyList<WhtCertificateModel> certificates)
        => Document.Create(container =>
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

    private void Compose(IContainer container, WhtCertificateModel c)
    {
        container.Column(col =>
        {
            // ── หัวเรื่อง + มุมขวา (ฉบับที่ 1) ──
            col.Item().Row(row =>
            {
                row.RelativeItem();
                row.RelativeItem(2).Column(t =>
                {
                    t.Item().AlignCenter().Text("หนังสือรับรองการหักภาษี ณ ที่จ่าย").Bold().FontSize(14);
                    t.Item().AlignCenter().Text("ตามมาตรา 50 ทวิ แห่งประมวลรัษฎากร").FontSize(10);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("ฉบับที่ 1\n").Bold().FontSize(9);
                    t.Span("สำหรับผู้ถูกหักภาษี ณ ที่จ่าย\nใช้แนบพร้อมกับแบบแสดงรายการภาษี").FontSize(8);
                });
            });

            col.Item().PaddingTop(2).Text(t =>
            {
                t.Span("ลำดับที่ *_________ ").FontSize(10);
                t.Span($"ในแบบยื่น {c.FormLabel}  เลขที่ ").FontSize(10);
                t.Span(c.SequenceNo).Bold().FontSize(10);
            });

            // ── คู่สัญญา ──
            col.Item().PaddingTop(6).Element(e => PartyBlock(e,
                "ผู้มีหน้าที่หักภาษี ณ ที่จ่าย", c.PayerTaxId, c.PayerName, c.PayerAddress));
            col.Item().PaddingTop(4).Element(e => PartyBlock(e,
                "ผู้ถูกหักภาษี ณ ที่จ่าย", c.PayeeTaxId, c.PayeeName, c.PayeeAddress));

            // ── ตารางประเภทเงินได้ (ครบ 1–6 ตามแบบ 50 ทวิ) ──
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(11);
                    cd.RelativeColumn(3);
                    cd.RelativeColumn(3);
                    cd.RelativeColumn(3);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HeadCell).Text("ประเภทเงินได้พึงประเมินที่จ่าย").FontSize(9);
                    h.Cell().Element(HeadCell).AlignCenter().Text("วัน เดือน ปี\nที่จ่าย").FontSize(8);
                    h.Cell().Element(HeadCell).AlignCenter().Text("จำนวนเงิน\nที่จ่าย").FontSize(8);
                    h.Cell().Element(HeadCell).AlignCenter().Text("ภาษีที่หักและ\nนำส่งไว้").FontSize(8);
                });

                Income(table, c, "1. เงินเดือน ค่าจ้าง เบี้ยเลี้ยง โบนัส ฯลฯ ตามมาตรา 40(1)", 1);
                Income(table, c, "2. ค่าธรรมเนียม ค่านายหน้า ฯลฯ ตามมาตรา 40(2)", 2);
                Income(table, c, "3. ค่าแห่งลิขสิทธิ์ ฯลฯ ตามมาตรา 40(3)", 3);
                Income(table, c, "4. (ก) ค่าดอกเบี้ย ฯลฯ ตามมาตรา 40(4)(ก)", 41);
                Income(table, c, "    (ข) เงินปันผล เงินส่วนแบ่งกำไร ฯลฯ ตามมาตรา 40(4)(ข)", 42);
                Note(table, "         (1) กรณีผู้ได้รับเงินปันผลได้รับเครดิตภาษี โดยจ่ายจาก");
                Note(table, "              กำไรสุทธิของกิจการที่ต้องเสียภาษีเงินได้นิติบุคคลในอัตราดังนี้");
                Note(table, "              (1.1) อัตราร้อยละ 30 ของกำไรสุทธิ");
                Note(table, "              (1.2) อัตราร้อยละ 25 ของกำไรสุทธิ");
                Note(table, "              (1.3) อัตราร้อยละ 20 ของกำไรสุทธิ");
                Note(table, "              (1.4) อัตราอื่นๆ (ระบุ) ........... ของกำไรสุทธิ");
                Note(table, "         (2) กรณีผู้ได้รับเงินปันผลไม่ได้รับเครดิตภาษี เนื่องจากจ่าย");
                Note(table, "              (2.1) กำไรสุทธิของกิจการที่ได้รับยกเว้นภาษี");
                Note(table, "              (2.2) เงินปันผลหรือเงินส่วนแบ่งของกำไรที่ได้รับยกเว้น ไม่ต้องนำมารวมคำนวณเป็นรายได้");
                Note(table, "              (2.3) กำไรสุทธิส่วนที่ได้หักผลขาดทุนสุทธิยกมาไม่เกิน 5 ปี ก่อนรอบระยะเวลาบัญชีปีปัจจุบัน");
                Note(table, "              (2.4) กำไรที่รับรู้ทางบัญชีโดยวิธีส่วนได้เสีย (equity method)");
                Note(table, "              (2.5) อื่นๆ (ระบุ) ........");
                Income(table, c, "5. การจ่ายเงินได้ที่ต้องหักภาษี ณ ที่จ่ายตามคำสั่งกรมสรรพากร" +
                    "ที่ออกตามมาตรา 3 เตรส (ระบุ) " + (c.IncomeCategory == 5 ? c.IncomeType : ""), 5);
                Note(table, "         (เช่น รางวัล ส่วนลดหรือประโยชน์ใดๆ เนื่องจากการส่งเสริมการขาย รางวัลในการประกวด การแข่งขัน");
                Note(table, "          การชิงโชค ค่าแสดงของนักแสดงสาธารณะ ค่าขนส่ง ค่าบริการ ค่าเบี้ยประกันวินาศภัย ฯลฯ)");
                Income(table, c, "6. อื่นๆ (ระบุ) " + (c.IncomeCategory == 6 ? c.IncomeType : ""), 6);

                // รวม
                table.Cell().Element(TotalCell).AlignRight().Text("รวมเงินที่จ่ายและภาษีที่หักนำส่ง").Bold().FontSize(9);
                table.Cell().Element(TotalCell).Text("");
                table.Cell().Element(TotalCell).AlignRight().Text(Money(c.Amount)).Bold().FontSize(9);
                table.Cell().Element(TotalCell).AlignRight().Text(Money(c.TaxAmount)).Bold().FontSize(9);
            });

            col.Item().PaddingTop(4).Text(txt =>
            {
                txt.Span("รวมเงินภาษีที่หักนำส่ง (ตัวอักษร)  ").Bold();
                txt.Span($"({c.AmountInWords})");
            });

            // ── ผู้จ่ายเงิน (เงื่อนไขการออกภาษี) ──
            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("ผู้จ่ายเงิน    ").Bold();
                t.Span($"{Chk(c.ConditionType == 1)} หักภาษี ณ ที่จ่าย     ");
                t.Span($"{Chk(c.ConditionType == 2)} ออกภาษีให้ตลอดไป     ");
                t.Span($"{Chk(c.ConditionType == 3)} ออกภาษีให้ครั้งเดียว     ");
                t.Span($"{Chk(c.ConditionType == 4)} อื่นๆ (ระบุ) ..............");
            });

            col.Item().PaddingTop(8).Text("ขอรับรองว่าข้อความและตัวเลขดังกล่าวข้างต้นถูกต้องตรงกับความเป็นจริงทุกประการ").FontSize(10);

            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(260).Column(s =>
                {
                    s.Item().AlignCenter().Text("ลงชื่อ ......................................................... ผู้มีหน้าที่หักภาษี ณ ที่จ่าย").FontSize(10);
                    s.Item().AlignCenter().PaddingTop(2).Text("(ประทับตรานิติบุคคล ถ้ามี)").FontSize(8);
                    s.Item().AlignCenter().PaddingTop(6).Text($"วันเดือนปีที่ออกหนังสือรับรองฯ  {FormatThaiDate(c.IssueDate)}").FontSize(10);
                });
            });

            // ── หมายเหตุ + คำเตือน ──
            col.Item().PaddingTop(10).Text(
                "หมายเหตุ  * ให้สามารถอ้างอิงหรือสอบยันกันได้ระหว่างลำดับที่ตามหนังสือรับรองฯ กับแบบยื่นรายการภาษีหัก ณ ที่จ่าย")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            col.Item().Text(
                "คำเตือน  ผู้มีหน้าที่ออกหนังสือรับรองการหักภาษี ณ ที่จ่าย ฝ่าฝืนไม่ปฏิบัติตามมาตรา 50 ทวิ แห่งประมวลรัษฎากร " +
                "ต้องรับโทษทางอาญาตามมาตรา 35 แห่งประมวลรัษฎากร")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
        });
    }

    /// <summary>แถวประเภทเงินได้ — เติมวันที่/จำนวนเงิน/ภาษี เฉพาะหมวดที่ตรงกับ IncomeCategory</summary>
    private void Income(TableDescriptor table, WhtCertificateModel c, string label, int category)
    {
        bool active = c.IncomeCategory == category;
        table.Cell().Element(BodyCell).Text(label).FontSize(9);
        table.Cell().Element(BodyCell).AlignCenter().Text(active ? FormatThaiDate(c.PayDate) : "").FontSize(9);
        table.Cell().Element(BodyCell).AlignRight().Text(active ? Money(c.Amount) : "").FontSize(9);
        table.Cell().Element(BodyCell).AlignRight().Text(active ? Money(c.TaxAmount) : "").FontSize(9);
    }

    /// <summary>แถวข้อความย่อย (ไม่มีช่องจำนวนเงิน)</summary>
    private static void Note(TableDescriptor table, string label)
    {
        table.Cell().Element(NoteCell).Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
        table.Cell().Element(NoteCell).Text("");
        table.Cell().Element(NoteCell).Text("");
        table.Cell().Element(NoteCell).Text("");
    }

    private static string Chk(bool on) => on ? "☑" : "☐";

    private static void PartyBlock(IContainer container, string title, string taxId, string name, string? address)
    {
        container.Border(0.6f).PaddingHorizontal(6).PaddingVertical(4).Column(col =>
        {
            col.Item().Text(txt =>
            {
                txt.Span($"{title}  ").Bold().FontSize(10);
                txt.Span($"เลขประจำตัวผู้เสียภาษี {taxId}").FontSize(10);
                txt.Span("     ☑ สำนักงานใหญ่   ☐ สาขา").FontSize(9);
            });
            col.Item().Text($"ชื่อ  {name}").FontSize(10);
            if (!string.IsNullOrWhiteSpace(address))
                col.Item().Text($"ที่อยู่  {address}").FontSize(9);
        });
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Border(0.6f).Background(Colors.Grey.Lighten3).PaddingVertical(3).PaddingHorizontal(4).DefaultTextStyle(t => t.SemiBold());

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.6f).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer NoteCell(IContainer c) =>
        c.BorderLeft(0.6f).BorderRight(0.6f).PaddingHorizontal(4).PaddingBottom(1);

    private static IContainer TotalCell(IContainer c) =>
        c.Border(0.6f).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4);

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
