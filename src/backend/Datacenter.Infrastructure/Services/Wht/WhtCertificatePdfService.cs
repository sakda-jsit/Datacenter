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
                    page.Margin(1.0f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily(_font).FontSize(11).LineHeight(1.25f));
                    page.Content().Element(e => Compose(e, c));
                });
            }
        });

    private void Compose(IContainer container, WhtCertificateModel c)
    {
        container.Column(col =>
        {
            // ── หัวเรื่อง ──
            col.Item().Row(row =>
            {
                row.RelativeItem();
                row.RelativeItem(2).Column(t =>
                {
                    t.Item().AlignCenter().Text("หนังสือรับรองการหักภาษี ณ ที่จ่าย").Bold().FontSize(14);
                    t.Item().AlignCenter().Text("ตามมาตรา 50 ทวิ แห่งประมวลรัษฎากร").FontSize(10);
                });
                row.RelativeItem();
            });

            // ── ลำดับที่ (ชิดซ้าย) | เลขที่ (ชิดขวา) บรรทัดเดียวกัน ──
            col.Item().PaddingTop(2).Row(r =>
            {
                r.RelativeItem().Text($"ลำดับที่ *_________ ในแบบ แบบยื่น {c.FormLabel}").FontSize(10);
                r.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("เลขที่ ").FontSize(10);
                    t.Span(c.SequenceNo).Bold().FontSize(10);
                });
            });

            // ── ประโยค "ฉบับที่ 1..." กึ่งกลาง ตัวหนา ──
            col.Item().PaddingTop(6).AlignCenter().Text(
                "ฉบับที่ 1. สำหรับผู้ถูกหักภาษี ณ ที่จ่าย ใช้แนบพร้อมกับแบบแสดงรายการภาษี").Bold().FontSize(11);

            // ── คู่สัญญา ──
            col.Item().PaddingTop(2).Element(e => PartyBlock(e,
                "ผู้มีหน้าที่หักภาษี ณ ที่จ่าย", c.PayerTaxId, c.PayerName, c.PayerAddress, BranchLabel(c.PayerBranchCode)));
            col.Item().PaddingTop(4).Element(e => PartyBlock(e,
                "ผู้ถูกหักภาษี ณ ที่จ่าย", c.PayeeTaxId, c.PayeeName, c.PayeeAddress, "สำนักงานใหญ่"));

            // ── ตารางประเภทเงินได้ (ครบ 1–6 ตามแบบ 50 ทวิ) ──
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(19f);   // ประเภทเงินได้พึงประเมินที่จ่าย (กว้าง — ข้อ 5 ไม่ตกบรรทัด)
                    cd.RelativeColumn(2.4f);  // วันเดือนปีที่จ่าย (พอให้ dd/mm/yy อยู่บรรทัดเดียว)
                    cd.RelativeColumn(3.3f);  // จำนวนเงินที่จ่าย
                    cd.RelativeColumn(3.0f);  // ภาษีที่หักและนำส่งไว้ (ขยายให้กว้างขึ้น)
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
                Note(table, "              ☐ (1.1) อัตราร้อยละ 30 ของกำไรสุทธิ");
                Note(table, "              ☐ (1.2) อัตราร้อยละ 25 ของกำไรสุทธิ");
                Note(table, "              ☐ (1.3) อัตราร้อยละ 20 ของกำไรสุทธิ");
                Note(table, "              ☐ (1.4) อัตราอื่นๆ (ระบุ) ........... ของกำไรสุทธิ");
                Note(table, "         (2) กรณีผู้ได้รับเงินปันผลไม่ได้รับเครดิตภาษี เนื่องจากจ่าย");
                Note(table, "              (2.1) กำไรสุทธิของกิจการที่ได้รับยกเว้นภาษี");
                Note(table, "              (2.2) เงินปันผลหรือเงินส่วนแบ่งของกำไรที่ได้รับยกเว้น ไม่ต้องนำมารวมคำนวณเป็นรายได้");
                Note(table, "              (2.3) กำไรสุทธิส่วนที่ได้หักผลขาดทุนสุทธิยกมาไม่เกิน 5 ปี ก่อนรอบระยะเวลาบัญชีปีปัจจุบัน");
                Note(table, "              (2.4) กำไรที่รับรู้ทางบัญชีโดยวิธีส่วนได้เสีย (equity method)");
                Note(table, "              (2.5) อื่นๆ (ระบุ) ........");
                Income(table, c, "5. การจ่ายเงินได้ที่ต้องหักภาษี ณ ที่จ่ายตามคำสั่งกรมสรรพากร" +
                    "ที่ออกตามมาตรา 3 เตรส (ระบุ)" + (c.IncomeCategory == 5 ? "\n       " + c.IncomeType : ""), 5);
                Note(table, "         (เช่น รางวัล ส่วนลดหรือประโยชน์ใดๆ เนื่องจากการส่งเสริมการขาย รางวัลในการประกวด การแข่งขัน");
                Note(table, "          การชิงโชค ค่าแสดงของนักแสดงสาธารณะ ค่าขนส่ง ค่าบริการ ค่าเบี้ยประกันวินาศภัย ฯลฯ)");
                Income(table, c, "6. อื่นๆ (ระบุ) " + (c.IncomeCategory == 6 ? c.IncomeType : ""), 6);

                // รวม
                table.Cell().Element(TotalCell).AlignRight().Text("รวมเงินที่จ่ายและภาษีที่หักนำส่ง").Bold().FontSize(9);
                table.Cell().Element(TotalCell).Text("");
                table.Cell().Element(TotalCell).AlignRight().Text(Money(c.Amount)).Bold().FontSize(9);
                table.Cell().Element(TotalCell).AlignRight().Text(Money0(c.TaxAmount)).Bold().FontSize(9);
            });

            col.Item().PaddingTop(3).Text(txt =>
            {
                txt.Span("รวมเงินภาษีที่หักนำส่ง (ตัวอักษร)  ").Bold();
                txt.Span($"({c.AmountInWords})");
            });

            // ── เฉพาะหนังสือรับรองเงินเดือน (ภ.ง.ด.1ก): กองทุนประกันสังคม + กองทุนสำรองเลี้ยงชีพ ──
            if (c.SsoContribution.HasValue)
            {
                col.Item().PaddingTop(3).Row(r =>
                {
                    r.RelativeItem().Text("เงินสะสมเข้ากองทุนประกันสังคม").FontSize(10);
                    r.ConstantItem(110).AlignRight().Text(Money(c.SsoContribution.Value)).FontSize(10);
                    r.ConstantItem(28).AlignRight().Text("บาท").FontSize(10);
                });
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("เงินสะสมจ่ายเข้ากองทุนสำรองเลี้ยงชีพ").FontSize(10);
                        t.Span("    ใบอนุญาตเลขที่ .................................").FontSize(9);
                    });
                    r.ConstantItem(110).AlignRight().Text(c.ProvidentFund is > 0 ? Money(c.ProvidentFund.Value) : "").FontSize(10);
                    r.ConstantItem(28).AlignRight().Text("บาท").FontSize(10);
                });
            }

            // ── ส่วนท้าย: ผู้จ่ายเงิน (ซ้าย) | ลงชื่อ-ประทับตรา (ขวา) ในกรอบเดียว ──
            col.Item().PaddingTop(4).Border(0.6f).Row(row =>
            {
                // ซ้าย: ผู้จ่ายเงิน + checkbox เรียงลง
                row.RelativeItem(5).BorderRight(0.6f).PaddingHorizontal(6).PaddingVertical(4).Column(l =>
                {
                    l.Item().Text("ผู้จ่ายเงิน").Bold().FontSize(10);
                    l.Item().PaddingTop(2).Text($"{Chk(c.ConditionType == 1)} หักภาษี ณ ที่จ่าย").FontSize(10);
                    l.Item().Text($"{Chk(c.ConditionType == 2)} ออกภาษีให้ตลอดไป").FontSize(10);
                    l.Item().Text($"{Chk(c.ConditionType == 3)} ออกภาษีให้ครั้งเดียว").FontSize(10);
                    l.Item().Text($"{Chk(c.ConditionType == 4)} อื่นๆ (ระบุ) ..................").FontSize(10);
                });
                // ขวา: ขอรับรอง + ลงชื่อ + ประทับตรา + วันที่
                row.RelativeItem(6).PaddingHorizontal(8).PaddingVertical(4).Column(r =>
                {
                    r.Item().Text("ขอรับรองว่าข้อความและตัวเลขดังกล่าวข้างต้น ถูกต้องตรงกับความเป็นจริงทุกประการ").FontSize(9);
                    // ลายเซ็น (ถ้ามี) วางเหนือเส้นจุดหลัง "ลงชื่อ" (เยื้องซ้าย) — ไม่มีก็เว้นที่เซ็นมือ
                    r.Item().PaddingTop(2).PaddingRight(95).Height(34).AlignCenter().AlignBottom().Element(sig =>
                    {
                        if (c.PayerSignature is { Length: > 0 })
                            // FitArea = พอดีทั้งกว้าง+สูง (กันลายเซ็นกว้างมากล้นเซลล์จน layout error)
                            sig.Image(c.PayerSignature).FitArea();
                    });
                    r.Item().AlignCenter().Text("ลงชื่อ ............................................. ผู้มีหน้าที่หักภาษี ณ ที่จ่าย").FontSize(10);
                    r.Item().PaddingTop(8).Row(sr =>
                    {
                        sr.RelativeItem().AlignCenter().Text($"วันเดือนปีที่ออกหนังสือรับรองฯ\n{FormatThaiDate(c.IssueDate)}").FontSize(9);
                        sr.ConstantItem(80).AlignCenter().Text("(ประทับตรา\nนิติบุคคล)").FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            // ── หมายเหตุ + คำเตือน ──
            col.Item().PaddingTop(6).Text(
                "หมายเหตุ  * ให้สามารถอ้างอิงหรือสอบยันกันได้ระหว่างลำดับที่ตามหนังสือรับรองฯ กับแบบยื่นรายการภาษีหัก ณ ที่จ่าย")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            col.Item().Text(
                "คำเตือน  ผู้มีหน้าที่ออกหนังสือรับรองการหักภาษี ณ ที่จ่าย ฝ่าฝืนไม่ปฏิบัติตามมาตรา 50 ทวิ แห่งประมวลรัษฎากร " +
                "ต้องรับโทษทางอาญาตามมาตรา 35 แห่งประมวลรัษฎากร")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
        });
    }

    /// <summary>แถวประเภทเงินได้ — เติมวันที่/จำนวนเงิน/ภาษี เฉพาะหมวดที่ตรงกับ IncomeCategory</summary>
    private void Income(TableDescriptor table, WhtCertificateModel c, string label, int category, float fontSize = 9f)
    {
        bool active = c.IncomeCategory == category;
        table.Cell().Element(BodyCell).Text(label).FontSize(fontSize);
        // จัดชิดล่าง — กรณี label หลายบรรทัด (ข้อ 5) ข้อมูลจะตรงกับบรรทัดสุดท้าย (ประเภทที่ระบุ)
        table.Cell().Element(BodyCell).AlignBottom().AlignCenter().Text(active ? FormatThaiDate(c.PayDate) : "").FontSize(9);
        table.Cell().Element(BodyCell).AlignBottom().AlignRight().Text(active ? Money(c.Amount) : "").FontSize(9);
        table.Cell().Element(BodyCell).AlignBottom().AlignRight().Text(active ? Money0(c.TaxAmount) : "").FontSize(9);
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

    /// <summary>"00000"/ว่าง = สำนักงานใหญ่ ; อื่นๆ = สาขา {code}</summary>
    private static string BranchLabel(string? branchCode)
    {
        var s = (branchCode ?? "").Trim().TrimStart('0');
        return s.Length == 0 ? "สำนักงานใหญ่" : $"สาขา {branchCode}";
    }

    private static void PartyBlock(IContainer container, string title, string taxId, string name, string? address, string branchLabel)
    {
        container.Border(0.6f).PaddingHorizontal(6).PaddingVertical(4).Column(col =>
        {
            // หัว: ชื่อหน้าที่ (ชิดซ้าย) | เลขประจำตัวผู้เสียภาษี (ชิดขวา)
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"{title} :").Bold().FontSize(10);
                r.RelativeItem().AlignRight().Text($"เลขประจำตัวผู้เสียภาษี  {taxId}").FontSize(10);
            });
            // ชื่อ (ซ้าย) | สำนักงานใหญ่/สาขา (ขวา)
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"ชื่อ  {name}").FontSize(10);
                r.RelativeItem().AlignRight().Text(branchLabel).FontSize(9);
            });
            if (!string.IsNullOrWhiteSpace(address))
                col.Item().Text($"ที่อยู่  {address}").FontSize(9);
        });
    }

    // หัวตาราง: กรอบเต็ม (เส้นบน=ขอบบนตาราง, เส้นล่าง=ใต้หัว) + เส้นแบ่งคอลัมน์
    private static IContainer HeadCell(IContainer c) =>
        c.Border(0.6f).Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(4).DefaultTextStyle(t => t.SemiBold());

    // แถวเนื้อหา: เฉพาะเส้นแบ่งคอลัมน์แนวตั้ง (ไม่มีเส้นแนวนอนระหว่างแถว)
    private static IContainer BodyCell(IContainer c) =>
        c.BorderVertical(0.6f).PaddingVertical(2.8f).PaddingHorizontal(4);

    private static IContainer NoteCell(IContainer c) =>
        c.BorderVertical(0.6f).PaddingHorizontal(4).PaddingVertical(1.6f);

    // แถวรวม: เส้นบน (ปิดท้ายรายการ) + เส้นล่าง (ขอบล่างตาราง) + เส้นแบ่งคอลัมน์
    private static IContainer TotalCell(IContainer c) =>
        c.Border(0.6f).Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(4);

    private static string Money(decimal v) =>
        v.ToString("#,##0.00", CultureInfo.InvariantCulture);

    /// <summary>เว้นว่างเมื่อค่าเป็น 0 (ตามแบบราชการที่ไม่พิมพ์ 0.00) — เช่น ภาษีเงินเดือนที่ไม่ถึงเกณฑ์</summary>
    private static string Money0(decimal v) => v == 0 ? "" : Money(v);

    /// <summary>dd/MM/ปีพ.ศ.(2 หลัก) เช่น 01/04/69</summary>
    private static string FormatThaiDate(DateTime? d)
    {
        if (d is null) return "";
        int beYear = d.Value.Year + 543;
        return $"{d.Value.Day:00}/{d.Value.Month:00}/{beYear % 100:00}";
    }
}
