using System.Globalization;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Payroll.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Datacenter.Infrastructure.Services.Payroll;

/// <summary>
/// PDF ฟอร์ม สปส.1-10 (แบบรายการแสดงการส่งเงินสมทบ) — หน้า 1 = ส่วนที่ 1 (สรุปนายจ้าง),
/// หน้า 2+ = ส่วนที่ 2 (รายชื่อผู้ประกันตน). ฟอนต์ไทยจากระบบ.
/// </summary>
public class SsoFilingPdfService(string fontFamily) : ISsoFilingPdfService
{
    private readonly string _font = string.IsNullOrWhiteSpace(fontFamily) ? "Tahoma" : fontFamily;

    private static readonly string[] MonthTh =
        ["", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
         "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];

    private static string Money(decimal v) => v.ToString("#,##0.00", CultureInfo.InvariantCulture);

    public byte[] Generate(SsoFilingDto d)
    {
        var monthYear = $"{MonthTh[d.Month]} พ.ศ. {d.Year + 543}";
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily(_font).FontSize(11).LineHeight(1.2f));
                page.Content().Element(e => Part1(e, d, monthYear));
            });
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily(_font).FontSize(11).LineHeight(1.2f));
                page.Content().Element(e => Part2(e, d, monthYear));
            });
        }).GeneratePdf();
    }

    // ── ส่วนที่ 1: สรุปนายจ้าง ──
    private void Part1(IContainer container, SsoFilingDto d, string monthYear)
    {
        container.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().AlignCenter().Text("แบบรายการแสดงการส่งเงินสมทบ").FontSize(15).Bold();
                r.ConstantItem(120).AlignRight().Text("สปส. 1-10 (ส่วนที่ 1)").FontSize(10);
            });
            col.Item().PaddingBottom(6).AlignCenter().Text("สำนักงานประกันสังคม").FontSize(10);

            col.Item().Text(t => { t.Span("ชื่อสถานประกอบการ  ").Bold(); t.Span(d.CompanyName); });
            col.Item().Text($"ที่ตั้งสำนักงานใหญ่/สาขา  {d.Address}");
            col.Item().PaddingBottom(4).Text($"รหัสไปรษณีย์  {d.PostalCode}      โทรศัพท์  {d.Phone}");

            col.Item().Row(r =>
            {
                r.RelativeItem().Text(t => { t.Span("เลขที่บัญชี  ").Bold(); t.Span(FormatAccount(d.SsoAccountNo)); });
                r.RelativeItem().Text(t => { t.Span("ลำดับที่สาขา  ").Bold(); t.Span(d.SsoBranchCode); });
                r.RelativeItem().Text(t => { t.Span("อัตราเงินสมทบร้อยละ  ").Bold(); t.Span(Money(d.RatePct)); });
            });

            col.Item().PaddingVertical(6).Text($"การนำส่งเงินสมทบสำหรับค่าจ้างเดือน  {monthYear}").Bold();

            // ตารางสรุป
            col.Item().Border(0.8f).Table(t =>
            {
                t.ColumnsDefinition(c => { c.RelativeColumn(7); c.RelativeColumn(3); });
                void Line(string label, decimal amount, bool bold = false)
                {
                    var l = t.Cell().BorderBottom(0.5f).PaddingVertical(3).PaddingHorizontal(6).Text(label);
                    if (bold) l.Bold();
                    var a = t.Cell().BorderBottom(0.5f).BorderLeft(0.5f).PaddingVertical(3).PaddingHorizontal(6)
                        .AlignRight().Text(Money(amount));
                    if (bold) a.Bold();
                }
                Line("1. เงินค่าจ้างทั้งสิ้น", d.TotalWage);
                Line("2. เงินสมทบผู้ประกันตน", d.TotalEmployee);
                Line("3. เงินสมทบนายจ้าง", d.TotalEmployer);
                Line("4. รวมเงินสมทบที่นำส่งทั้งสิ้น", d.GrandTotal, true);
                t.Cell().ColumnSpan(2).BorderBottom(0.5f).PaddingVertical(3).PaddingHorizontal(6)
                    .AlignCenter().Text($"( {d.GrandTotalText} )");
                t.Cell().PaddingVertical(3).PaddingHorizontal(6).Text("5. จำนวนผู้ประกันตนที่ส่งเงินสมทบ");
                t.Cell().BorderLeft(0.5f).PaddingVertical(3).PaddingHorizontal(6)
                    .AlignRight().Text($"{d.InsuredCount} คน");
            });

            col.Item().PaddingTop(10).Text("ข้าพเจ้าขอรับรองว่ารายการที่แจ้งไว้เป็นรายการที่ถูกต้องครบถ้วนและเป็นจริงทุกประการ");
            col.Item().PaddingTop(4).Text("พร้อมนี้ได้แนบ  ☒ อินเตอร์เน็ต   ☐ รายละเอียดการนำส่งเงินสมทบ   ☐ แผ่นจากแม่เหล็ก   ☐ อื่นๆ");

            col.Item().PaddingTop(40).Row(r =>
            {
                r.RelativeItem();
                r.ConstantItem(280).Column(s =>
                {
                    s.Item().AlignCenter().Text("ลงชื่อ ......................................................... นายจ้าง/ผู้รับมอบอำนาจ");
                    s.Item().PaddingTop(6).AlignCenter().Text("( ......................................................... )");
                    s.Item().PaddingTop(6).AlignCenter().Text("ตำแหน่ง .........................................................");
                    s.Item().PaddingTop(6).AlignCenter().Text("ยื่นแบบวันที่ ............ เดือน ...................... พ.ศ. ............");
                });
            });

            col.Item().PaddingTop(16).Text("หมายเหตุ: ทำรายการ สปส.1-10 ส่วนที่ 2 ผ่านระบบบริการอิเล็กทรอนิกส์ (www.sso.go.th/eservices)")
                .FontSize(8).Italic();
        });
    }

    // ── ส่วนที่ 2: รายชื่อผู้ประกันตน ──
    private void Part2(IContainer container, SsoFilingDto d, string monthYear)
    {
        container.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().AlignCenter().Text("แบบรายการแสดงการส่งเงินสมทบ").FontSize(15).Bold();
                r.ConstantItem(120).AlignRight().Text("สปส.1-10 (ส่วนที่ 2)").FontSize(10);
            });
            col.Item().PaddingBottom(4).AlignCenter().Text($"การนำส่งเงินสมทบสำหรับค่าจ้างเดือน  {monthYear}").FontSize(10);

            col.Item().Row(r =>
            {
                r.RelativeItem(2).Text(t => { t.Span("ชื่อสถานประกอบการ  ").Bold(); t.Span(d.CompanyName); });
                r.RelativeItem().Text(t => { t.Span("เลขที่บัญชี  ").Bold(); t.Span(FormatAccount(d.SsoAccountNo)); });
                r.RelativeItem().Text(t => { t.Span("สาขา  ").Bold(); t.Span(d.SsoBranchCode); });
            });

            col.Item().PaddingTop(6).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(35);   // ลำดับ
                    c.ConstantColumn(110);  // เลขบัตร
                    c.RelativeColumn();     // ชื่อ-สกุล
                    c.ConstantColumn(90);   // ค่าจ้าง
                    c.ConstantColumn(80);   // เงินสมทบ
                });
                IContainer H(IContainer x) => x.BorderBottom(1).PaddingVertical(4).PaddingHorizontal(4);
                t.Header(h =>
                {
                    h.Cell().Element(H).AlignCenter().Text("ลำดับที่").Bold();
                    h.Cell().Element(H).Text("เลขประจำตัวประชาชน").Bold();
                    h.Cell().Element(H).AlignCenter().Text("ชื่อ-ชื่อสกุล").Bold();
                    h.Cell().Element(H).AlignRight().Text("ค่าจ้าง").Bold();
                    h.Cell().Element(H).AlignRight().Text("เงินสมทบ").Bold();
                });
                foreach (var row in d.Rows)
                {
                    IContainer C(IContainer x) => x.BorderBottom(0.4f).PaddingVertical(2.5f).PaddingHorizontal(4);
                    t.Cell().Element(C).AlignCenter().Text(row.Seq.ToString());
                    t.Cell().Element(C).Text(row.NationalId);
                    t.Cell().Element(C).Text($"{row.Prefix}{row.FirstName} {row.LastName}");
                    t.Cell().Element(C).AlignRight().Text(Money(row.Wage));
                    t.Cell().Element(C).AlignRight().Text(Money(row.Contribution));
                }
                // ยอดรวม
                t.Cell().ColumnSpan(3).BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text("ยอดรวม").Bold();
                t.Cell().BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Money(d.TotalWage)).Bold();
                t.Cell().BorderTop(1).PaddingVertical(4).PaddingHorizontal(4).AlignRight().Text(Money(d.TotalEmployee)).Bold();
            });
        });
    }

    /// <summary>จัดรูปเลขบัญชี 10 หลัก → 2x-xxxxxxx-x</summary>
    private static string FormatAccount(string acc)
    {
        var s = new string((acc ?? "").Where(char.IsDigit).ToArray());
        return s.Length == 10 ? $"{s[..2]}-{s.Substring(2, 7)}-{s[9]}" : acc ?? "";
    }
}
