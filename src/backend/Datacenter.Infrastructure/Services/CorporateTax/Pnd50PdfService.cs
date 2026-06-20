using System.Globalization;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Domain.Enums;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.AcroForms;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Pdf.IO;

namespace Datacenter.Infrastructure.Services.CorporateTax;

/// <summary>
/// เติมแบบ ภ.ง.ด.50 (PDF) โดย overlay ข้อความตามพิกัดฟิลด์บน template CIT50.pdf ด้วยฟอนต์ไทย (Tahoma).
/// พิกัด (top-left origin, point) ดึงจาก AcroForm ของฟอร์มจริง — เฟส A หน้า 1 (หัว) + หน้า 2 (คำนวณภาษี).
/// </summary>
public class Pnd50PdfService : IPnd50PdfService
{
    private readonly string _templatePath;

    /// <summary>พิกัดกึ่งกลาง (x, point) ของ 13 ช่องเลขประจำตัวผู้เสียภาษีบนฟอร์ม CIT50 (วัดจาก raster grid)</summary>
    private static readonly double[] TaxIdCellCenters =
        [156.7, 173.3, 184.8, 201.3, 218.4, 230.3, 242.1, 260.9, 272.6, 284.4, 296.2, 307.9, 326.0];

    /// <summary>พิกัดกึ่งกลาง 13 ช่องเลขผู้เสียภาษีของผู้สอบบัญชี/ผู้ทำบัญชี (field 43/50 ฝั่งซ้าย) — วัดจากเส้นแบ่งทุกเส้น (กลุ่ม 1-4-5-2-1)</summary>
    private static readonly double[] AuditorTaxIdCellCenters =
        [43.5, 60.8, 72.4, 84.0, 95.7, 113.0, 124.6, 136.2, 147.9, 159.5, 177.1, 188.7, 206.1];

    /// <summary>พิกัดกึ่งกลาง 13 ช่องเลขผู้เสียภาษีของสำนักงานสอบบัญชี/ทำบัญชี (field 49/52 ฝั่งขวา) — วัดจากเส้นแบ่งทุกเส้น</summary>
    private static readonly double[] FirmTaxIdCellCenters =
        [402.7, 420.0, 431.6, 443.2, 454.9, 472.2, 483.8, 495.4, 507.1, 518.7, 536.3, 547.9, 565.3];

    public Pnd50PdfService(string fontPath)
    {
        // ตั้ง global font resolver ครั้งเดียว (กันตั้งซ้ำ)
        Pnd50FontResolver.EnsureRegistered(fontPath);
        _templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "CIT50.pdf");
    }

    public byte[] Build(Pnd50FormData d)
    {
        using var input = File.OpenRead(_templatePath);
        var doc = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        var font = new XFont("Tahoma", 9, XFontStyle.Regular);
        var p1 = XGraphics.FromPdfPage(doc.Pages[0], XGraphicsPdfPageOptions.Append);
        var p2 = XGraphics.FromPdfPage(doc.Pages[1], XGraphicsPdfPageOptions.Append);

        // ── หน้า 1: หัวแบบ (ฟอร์มฉบับใหม่ 2568) ──
        DrawDigitsAtCenters(p1, font, Digits(d.TaxId), TaxIdCellCenters, 86.8, 16.0);             // f1 เลขผู้เสียภาษี
        DrawText(p1, font, d.CompanyName, 41.2, 113.1, 290, 13.0, XStringFormats.CenterLeft);     // f2 ชื่อ
        // ที่ตั้งสำนักงาน (แยกช่อง)
        DrawText(p1, font, d.HouseNo, 148.0, 153.5, 36.5, 11.0, XStringFormats.CenterLeft);       // f7 เลขที่
        DrawText(p1, font, d.Moo, 204.3, 152.8, 13.9, 11.0, XStringFormats.Center);               // f8 หมู่ที่
        DrawText(p1, font, d.Soi, 256.7, 152.4, 74.0, 12.8, XStringFormats.CenterLeft);           // f9 ตรอก/ซอย
        DrawText(p1, font, d.Road, 115.7, 165.1, 65.5, 12.8, XStringFormats.CenterLeft);          // Text10.1 ถนน
        DrawText(p1, font, d.SubDistrict, 229.9, 166.7, 106.7, 12.8, XStringFormats.CenterLeft);  // f11 ตำบล/แขวง
        DrawText(p1, font, d.District, 69.2, 182.5, 115.8, 12.8, XStringFormats.CenterLeft);      // f12 อำเภอ/เขต
        DrawText(p1, font, d.Province, 215.5, 182.1, 118.9, 12.8, XStringFormats.CenterLeft);     // f13 จังหวัด
        DrawComb(p1, font, Digits(d.PostalCode), 81.4, 197.9, 60.1, 13.0, 5);                     // f14 รหัสไปรษณีย์
        DrawText(p1, font, d.Phone, 180.9, 196.8, 153.2, 12.8, XStringFormats.CenterLeft);        // f15 โทรศัพท์

        // ประกอบกิจการ (field 23.1) + รหัส ISIC (field 24, comb 6)
        DrawText(p1, font, d.BusinessActivity, 349.6, 357.5, 129.1, 13.0, XStringFormats.CenterLeft);
        DrawComb(p1, font, Digits(d.IsicCode), 496.9, 356.9, 72.6, 13.0, 6);

        // ผู้ตรวจสอบและรับรองบัญชี (ล่างหน้า 1): taxId (f43) / ชื่อ (f44) / ทะเบียน (f45 comb8)
        DrawDigitsAtCenters(p1, font, Digits(d.AuditorTaxId), AuditorTaxIdCellCenters, 730.4, 16.5);
        DrawText(p1, font, d.AuditorName, 216.6, 730.5, 250.0, 12.8, XStringFormats.CenterLeft);
        DrawComb(p1, font, Digits(d.AuditorLicenseNo), 471.9, 730.1, 95.7, 12.1, 8);
        // เลขผู้เสียภาษีสำนักงานสอบบัญชี (f49)
        DrawDigitsAtCenters(p1, font, Digits(d.AuditFirmTaxId), FirmTaxIdCellCenters, 755.2, 16.5);
        // วันที่ในรายงานผู้สอบ (f46 วัน / f47 เดือน / f48 พ.ศ.)
        if (d.AuditorSignDate is { } sd)
        {
            DrawComb(p1, font, sd.Day.ToString("00"), 232.9, 753.7, 23.9, 14.0, 2);
            DrawComb(p1, font, sd.Month.ToString("00"), 282.6, 754.0, 23.5, 14.0, 2);
            DrawComb(p1, font, (sd.Year + 543).ToString(), 329.4, 755.1, 48.2, 14.0, 4);
        }
        // ผู้ทำบัญชี: taxId (f50) + ชื่อ (44-2)
        DrawDigitsAtCenters(p1, font, Digits(d.BookkeeperTaxId), AuditorTaxIdCellCenters, 802.4, 16.5);
        DrawText(p1, font, d.BookkeeperName, 215.0, 800.7, 182.1, 12.8, XStringFormats.CenterLeft);
        // เลขผู้เสียภาษีสำนักงานทำบัญชี (f52)
        DrawDigitsAtCenters(p1, font, Digits(d.BookkeepingFirmTaxId), FirmTaxIdCellCenters, 801.4, 16.5);

        // (1) ยื่นปกติ (Group1) + สถานภาพ (1) ตั้งขึ้นตามกฎหมายไทย (Group00) — set ค่า field ให้ widget ติ๊กเอง
        SetRadio(doc, "Group1", "Choice1");
        SetRadio(doc, "Group00", "Choice1");

        // อีเมล: ผู้ประกอบการ (บริษัท) / ผู้ทำบัญชี (สำนักงานบัญชี) — บนเส้นประหลัง label (x เริ่มหลังคำต่างกัน)
        DrawText(p1, font, d.BusinessEmail, 165.0, 227.0, 169.0, 13.0, XStringFormats.CenterLeft);
        DrawText(p1, font, d.BookkeeperEmail, 148.0, 242.0, 186.0, 13.0, XStringFormats.CenterLeft);

        // ม.71ทวิ (รายได้ระหว่างกัน): เกิน 200 ล้าน → "มี" (Group06) / ไม่เกิน → "ไม่มี" (Group07)
        SetRadio(doc, d.RevenueOver200M ? "Group06" : "Group07", "Choice1");
        // รอบบัญชี ตั้งแต่/ถึง (comb 2/2/4)
        DrawComb(p1, font, d.PeriodStart.Day.ToString("00"), 399.8, 97.9, 24.0, 12.5, 2);
        DrawComb(p1, font, d.PeriodStart.Month.ToString("00"), 453.9, 97.4, 24.9, 12.5, 2);
        DrawComb(p1, font, (d.PeriodStart.Year + 543).ToString(), 511.1, 96.9, 46.7, 12.5, 4);
        DrawComb(p1, font, d.PeriodEnd.Day.ToString("00"), 399.6, 127.6, 23.5, 12.5, 2);
        DrawComb(p1, font, d.PeriodEnd.Month.ToString("00"), 453.3, 128.5, 24.4, 12.5, 2);
        DrawComb(p1, font, (d.PeriodEnd.Year + 543).ToString(), 511.3, 127.5, 47.1, 12.5, 4);

        // ── หน้า 2: การคำนวณภาษี (ขวา = จำนวนเงิน) ──
        // ── หน้า 2: รายการที่ 1 การคำนวณภาษี (ฟอร์มใหม่ 2568) ── ฐานภาษีไปอยู่หน้า 3 (บรรทัด 21)
        DrawMoneyComb(p2, font, d.TaxAmount, 567.2, 458.3, 16.9);        // f50 ภาษีที่คำนวณได้
        DrawMoneyComb(p2, font, d.WhtCredit, 433.4, 524.3, 16.9);        // f54 ภาษีหัก ณ ที่จ่าย
        DrawMoneyComb(p2, font, d.TotalCredit, 567.2, 590.5, 16.9);      // f57 รวมรายการหัก
        DrawMoneyComb(p2, font, Math.Abs(d.NetPayable), 567.2, 612.6, 16.9); // f58 คงเหลือ
        DrawMoneyComb(p2, font, Math.Abs(d.NetPayable), 567.2, 656.0, 16.9); // f61 รวม

        // checkbox: กำไร/ขาดทุนสุทธิ (Group5): Choice1=กำไร / Choice2=ขาดทุน
        SetRadio(doc, "Group5", d.IsNetProfit ? "Choice1" : "Choice2");
        // การคำนวณภาษี (Group21): SME → (2) ลดอัตราภาษี (Choice2) + SMEs (Group6 Choice1); อื่น → (1) กรณีทั่วไป (Choice1)
        if (d.RateScheme == TaxRateScheme.SmeTiered)
        {
            SetRadio(doc, "Group21", "Choice2");  // (2) กรณีลดอัตราภาษี
            SetRadio(doc, "Group6", "Choice1");   // SMEs
        }
        else SetRadio(doc, "Group21", "Choice1"); // (1) กรณีทั่วไป
        // คงเหลือ (Group7) + รวม (Group8): ชำระเพิ่มเติม (Choice1, ≥0) / ชำระไว้เกิน (Choice2, <0)
        var payState = d.NetPayable >= 0 ? "Choice1" : "Choice2";
        SetRadio(doc, "Group7", payState);
        SetRadio(doc, "Group8", payState);

        // ── หน้า 3: รายการที่ 3 — reconciliation กำไรบัญชี → เงินได้สุทธิเพื่อเสียภาษี ──
        if (d.Page3 is { } p3d && doc.Pages.Count > 2)
        {
            var p3 = XGraphics.FromPdfPage(doc.Pages[2], XGraphicsPdfPageOptions.Append);
            // เติมคอลัมน์ 2 (กิจการที่ต้องเสียภาษี) + คอลัมน์ 3 (รวม) เท่ากัน; คอลัมน์ 1 (ยกเว้น) เว้น
            void Row(double y, decimal v)
            {
                DrawMoneyComb(p3, font, v, 463.7, y, 17.5);   // col2 เสียภาษี (wall 463.7 — กริดช่องเยื้องซ้ายกว่า col3)
                DrawMoneyComb(p3, font, v, 571.5, y, 17.5);   // col3 รวม (wall 571.5)
            }
            Row(124.0, p3d.Revenue);             // 1. รายได้โดยตรง
            Row(155.7, p3d.Cogs);                // 2. หัก ต้นทุนขาย
            Row(176.6, p3d.GrossProfit);         // 3. กำไร(ขาดทุน)ขั้นต้น
            Row(196.5, p3d.OtherIncome);         // 4. บวก รายได้อื่น
            Row(217.6, p3d.GrossProfit + p3d.OtherIncome);  // 5. รวม (3+4)
            Row(258.3, p3d.GrossProfit + p3d.OtherIncome);  // 7. รวม (5-6)
            Row(276.2, p3d.Sga);                 // 8. หัก รายจ่ายขายและบริหาร
            Row(294.7, p3d.NetAccountingProfit); // 9. กำไร(ขาดทุน)สุทธิตามบัญชี
            Row(343.4, p3d.AddBack);             // 11. บวก รายจ่ายต้องห้าม
            Row(366.0, p3d.NetAccountingProfit + p3d.AddBack); // 12. รวม
            Row(397.3, p3d.Deduction);           // 13. หัก รายได้ยกเว้น/หักเพิ่ม
            Row(419.0, p3d.AdjustedProfit);      // 14. รวม
            Row(449.5, p3d.LossUsed);            // 15. หัก ขาดทุนยกมา
            Row(477.7, p3d.NetTaxableIncome);    // 16. รวม
            DrawMoneyComb(p3, font, p3d.NetTaxableIncome, 571.5, 594.8, 17.5); // 21. เงินได้สุทธิเพื่อเสียภาษี (col3)

            // checkbox กำไร/ขาดทุน (Group100 L3 / Group101 L9 ใช้ on-state "0"/"1" ; Group9 L21 ใช้ Choice1/2)
            SetRadio(doc, "Group100", p3d.GrossProfit >= 0 ? "0" : "1");
            SetRadio(doc, "Group101", p3d.NetAccountingProfit >= 0 ? "0" : "1");
            SetRadio(doc, "Group9", p3d.NetTaxableIncome > 0 ? "Choice1" : "Choice2");
        }

        // ── หน้า 6 (index 5): รายการที่ 9 งบดุล + ความเห็นผู้สอบบัญชี ──
        if (doc.Pages.Count > 5 && (d.Page7 is not null || !string.IsNullOrWhiteSpace(d.AuditorName)))
        {
            var p7g = XGraphics.FromPdfPage(doc.Pages[5], XGraphicsPdfPageOptions.Append);
          if (d.Page7 is { } p7)
          {
            void Bs(double y, decimal v) => DrawMoneyComb(p7g, font, v, 563.0, y, 16.9); // wall 563.0
            Bs(74.7, p7.Cash);                 // เงินสด
            Bs(91.7, p7.Ar);                   // ลูกหนี้การค้า
            Bs(111.0, p7.Inventory);           // สินค้าคงเหลือ
            Bs(128.3, p7.OtherCurrentAsset);   // สินทรัพย์หมุนเวียนอื่น
            Bs(164.7, p7.LoansToRelated);      // เงินให้กู้ยืมบุคคลที่เกี่ยวข้อง
            Bs(181.8, p7.Ppe);                 // ที่ดิน อาคาร อุปกรณ์-สุทธิ
            Bs(201.0, p7.OtherAssetNet);       // ทรัพย์สินอื่น-สุทธิ
            Bs(237.9, p7.OtherNonCurrentAsset);// สินทรัพย์ไม่หมุนเวียนอื่น
            Bs(256.2, p7.TotalAssets);         // รวมสินทรัพย์
            Bs(297.9, p7.BankOdShortLoan);     // เบิกเกินบัญชี+กู้ระยะสั้น
            Bs(316.2, p7.Ap);                  // เจ้าหนี้การค้า
            Bs(333.8, p7.CurrentLoan);         // เงินกู้ยืม
            Bs(351.1, p7.OtherCurrentLiab);    // หนี้สินหมุนเวียนอื่น
            Bs(388.1, p7.LongTermLoan);        // เงินกู้ยืมระยะยาว
            Bs(405.9, p7.OtherNonCurrentLiab); // หนี้สินไม่หมุนเวียนอื่น
            Bs(424.4, p7.TotalLiabilities);    // รวมหนี้สิน
            Bs(462.2, p7.PaidUpCapital);       // ทุนที่ออกและชำระแล้ว
            Bs(499.9, p7.RetainedEarnings);    // กำไร/ขาดทุนสะสม
            Bs(517.8, p7.TotalEquity);         // รวมส่วนของผู้ถือหุ้น
            Bs(535.5, p7.TotalLiabAndEquity);  // รวมหนี้สิน+ทุน
            // checkbox กำไร/ขาดทุนสะสม (Group91): Choice1=กำไรสะสม / Choice2=ขาดทุนสะสม
            SetRadio(doc, "Group91", p7.IsRetainedProfit ? "Choice1" : "Choice2");
          }

            // ความเห็นของผู้สอบบัญชีรับอนุญาต (Group92): 1=ไม่มีเงื่อนไข 2=มีเงื่อนไข 3=ไม่แสดงความเห็น 4=ไม่ถูกต้อง
            if (!string.IsNullOrWhiteSpace(d.AuditorName))
            {
                var opinion = d.AuditorOpinion is >= 1 and <= 4 ? d.AuditorOpinion : 1;
                SetRadio(doc, "Group92", "Choice" + opinion);
            }
        }

        // ── schedule cells (รายการ 8 ฯลฯ จาก mapping บัญชี→CIT50) — วาดตามพิกัด ──
        if (d.ScheduleCells is { Count: > 0 } cells)
        {
            var gByPage = new Dictionary<int, XGraphics>();
            foreach (var c in cells)
            {
                if (c.Page < 0 || c.Page >= doc.Pages.Count) continue;
                if (!gByPage.TryGetValue(c.Page, out var g))
                    gByPage[c.Page] = g = XGraphics.FromPdfPage(doc.Pages[c.Page], XGraphicsPdfPageOptions.Append);
                if (c.Amount != 0) DrawMoneyComb(g, font, c.Amount, c.X + c.W, c.Y, 16.9); // wall = ขอบขวาช่อง
            }
        }

        using var output = new MemoryStream();
        doc.Save(output);
        return output.ToArray();
    }

    private static string Digits(string? s)
        => string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    private static void DrawText(XGraphics g, XFont f, string? text, double x, double y, double w, double h, XStringFormat fmt)
    {
        if (string.IsNullOrEmpty(text)) return;
        // เว้นช่องเล็กน้อยหลัง label (inset) + ขยับลงให้ baseline นั่งใกล้เส้น (ไม่ลอยสูง) — กึ่งกลางคงเดิม
        g.DrawString(text, f, XBrushes.Black, new XRect(x + 2.5, y + 1.8, w - 5.0, h), fmt);
    }

    /// <summary>
    /// ติ๊ก radio/checkbox โดย "set ค่า field" บน AcroForm — widget จะ render เครื่องหมายเอง (native).
    /// เชื่อถือได้กว่าการวาดทับ เพราะ widget appearance อยู่เหนือ page content (กล่องบางตัวมี Off-appearance ทึบบัง overlay).
    /// <paramref name="onState"/> = ชื่อ on-state ของฟอร์ม (เช่น "Choice1", "0", "1").
    /// </summary>
    private static void SetRadio(PdfDocument doc, string fieldName, string onState)
    {
        var form = doc.AcroForm;
        if (form is null) return;
        PdfAcroField? f;
        try { f = form.Fields[fieldName]; } catch { return; }
        if (f is null) return;

        f.Elements.SetName("/V", onState);

        var kids = f.Elements.GetArray("/Kids");
        if (kids is { Elements.Count: > 0 })
        {
            foreach (var item in kids.Elements)
            {
                if ((item is PdfReference r ? r.Value : item) is not PdfDictionary kid) continue;
                kid.Elements.SetName("/AS", KidHasState(kid, onState) ? onState : "Off");
            }
        }
        else
        {
            f.Elements.SetName("/AS", onState); // widget เดี่ยว (merged) — set บน field เอง
        }
    }

    /// <summary>kid widget นี้มี appearance ของ on-state ที่ขอหรือไม่ (ถ้ามี = ตัวที่ต้องติ๊ก)</summary>
    private static bool KidHasState(PdfDictionary kid, string onState)
    {
        var n = kid.Elements.GetDictionary("/AP")?.Elements.GetDictionary("/N");
        return n is not null && n.Elements.ContainsKey("/" + onState);
    }

    private static void DrawMoney(XGraphics g, XFont f, decimal v, double x, double y, double w, double h)
        => DrawText(g, f, v.ToString("#,##0.00", CultureInfo.InvariantCulture), x, y, w - 3, h, XStringFormats.CenterRight);

    /// <summary>ระยะห่างหนึ่งช่องของกริดจำนวนเงินที่พิมพ์บนฟอร์ม CIT50 (point)</summary>
    private const double MoneyCombPitch = 7.9;

    /// <summary>
    /// PdfSharp's <see cref="XStringFormats.Center"/> วาง "กล่องบรรทัด" (รวมช่อง descender) ไว้กลางกรอบ
    /// ตัวเลขไม่มีหาง (descender) จึงดูลอยสูงกว่ากลางช่องจริง ~0.12em — ขยับลงให้ตัวเลขอยู่กึ่งกลางช่องพอดี (วัดจาก template 9pt ≈ 1.0pt).
    /// </summary>
    private static double DigitVNudge(XFont f) => f.Size * 0.12;

    /// <summary>
    /// วาดจำนวนเงินทีละหลักลง "ช่อง" (comb) ที่พิมพ์ไว้บนฟอร์ม — หลักจำนวนเต็มชิดขวา + 2 ช่องสตางค์แยกขวาสุด.
    /// <paramref name="wall"/> = พิกัด x ของผนังขวาสุดของกล่องสตางค์ (ขอบกริดจริง วัดจาก template).
    /// เรขาคณิตเดียวทั้งฟอร์ม: pitch 7.9 / สตางค์ 2 ช่อง / ช่องว่างระหว่างจำนวนเต็มกับสตางค์ 3.2.
    /// </summary>
    private static void DrawMoneyComb(XGraphics g, XFont f, decimal value, double wall, double y, double h)
    {
        var s = Math.Abs(value).ToString("0.00", CultureInfo.InvariantCulture); // ไม่มีตัวคั่นพัน (กริดแบ่งหลักด้วยเส้นพิมพ์)
        int dot = s.IndexOf('.');
        string ip = s[..dot], dp = s[(dot + 1)..];

        void Cell(string ch, double cx)
            => g.DrawString(ch, f, XBrushes.Black, new XRect(cx - MoneyCombPitch / 2, y + DigitVNudge(f), MoneyCombPitch, h), XStringFormats.Center);

        Cell(dp[1].ToString(), wall - 3.95);   // สตางค์ หลัก 1/100 (ช่องขวาสุด)
        Cell(dp[0].ToString(), wall - 11.85);  // สตางค์ หลัก 1/10
        double x = wall - 22.95;               // หลักหน่วยของจำนวนเต็ม (ข้ามช่องว่าง 3.2 + กล่องสตางค์)
        for (int i = ip.Length - 1; i >= 0; i--, x -= MoneyCombPitch) Cell(ip[i].ToString(), x);
        if (value < 0) Cell("-", x);           // เครื่องหมายลบหน้าหลักซ้ายสุด
    }

    /// <summary>วาดตัวเลขลงช่อง comb ที่กว้างเท่ากัน — กึ่งกลางแต่ละช่อง</summary>
    private static void DrawComb(XGraphics g, XFont f, string text, double x, double y, double w, double h, int cells)
    {
        if (string.IsNullOrEmpty(text)) return;
        var cellW = w / cells;
        for (int i = 0; i < text.Length && i < cells; i++)
            g.DrawString(text[i].ToString(), f, XBrushes.Black,
                new XRect(x + i * cellW, y + DigitVNudge(f), cellW, h), XStringFormats.Center);
    }

    /// <summary>วาดแต่ละหลักกึ่งกลางช่องตามพิกัด x ที่กำหนด (ช่องไม่เท่ากัน)</summary>
    private static void DrawDigitsAtCenters(XGraphics g, XFont f, string text, double[] centers, double y, double h)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length && i < centers.Length; i++)
            g.DrawString(text[i].ToString(), f, XBrushes.Black,
                new XRect(centers[i] - 5, y + DigitVNudge(f), 10, h), XStringFormats.Center);
    }
}

/// <summary>โหลดฟอนต์ไทย (Tahoma) ให้ PdfSharpCore (overlay ข้อความไทยได้).</summary>
internal sealed class Pnd50FontResolver : IFontResolver
{
    private static Pnd50FontResolver? _instance;
    private static readonly object Lock = new();
    private readonly byte[] _fontData;

    private Pnd50FontResolver(byte[] fontData) => _fontData = fontData;

    public static void EnsureRegistered(string fontPath)
    {
        lock (Lock)
        {
            if (_instance is not null) return;
            var path = File.Exists(fontPath) ? fontPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "tahoma.ttf");
            _instance = new Pnd50FontResolver(File.ReadAllBytes(path));
            GlobalFontSettings.FontResolver = _instance;
        }
    }

    public string DefaultFontName => "Tahoma";

    public byte[] GetFont(string faceName) => _fontData;

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        => new("Tahoma");
}
