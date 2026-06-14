using System.Globalization;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.CorporateTax.DTOs;
using Datacenter.Domain.Enums;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
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

        // ── หน้า 1: หัวแบบ ──
        // เลขประจำตัวผู้เสียภาษี — วางแต่ละหลักกึ่งกลางช่องจริง (ช่องไม่เท่ากัน มีช่องคั่นกลุ่ม X-XXXX-XXXXX-XX-X)
        DrawDigitsAtCenters(p1, font, Digits(d.TaxId), TaxIdCellCenters, 88.6, 16.9);
        // ชื่อบริษัท
        DrawText(p1, font, d.CompanyName, 42, 115.9, 291, 12.6, XStringFormats.CenterLeft);
        // ที่ตั้งสำนักงาน (แยกช่อง)
        DrawText(p1, font, d.HouseNo, 150.5, 152.4, 37.8, 10.6, XStringFormats.CenterLeft);       // f7 เลขที่
        DrawText(p1, font, d.Moo, 206.6, 151.3, 12.1, 10.6, XStringFormats.Center);               // f8 หมู่ที่
        DrawText(p1, font, d.Soi, 256.2, 149.2, 77.8, 12.8, XStringFormats.CenterLeft);           // f9 ตรอก/ซอย
        DrawText(p1, font, d.Road, 45.4, 164.9, 124.8, 12.8, XStringFormats.CenterLeft);          // f10 ถนน
        DrawText(p1, font, d.SubDistrict, 214.1, 164.9, 119.9, 12.8, XStringFormats.CenterLeft);  // f11 ตำบล/แขวง
        DrawText(p1, font, d.District, 69.4, 180.5, 119.9, 12.8, XStringFormats.CenterLeft);       // f12 อำเภอ/เขต
        DrawText(p1, font, d.Province, 214.7, 180.5, 119.9, 12.8, XStringFormats.CenterLeft);     // f13 จังหวัด
        DrawComb(p1, font, Digits(d.PostalCode), 82.7, 197.0, 59.2, 13.2, 5);                     // f14 รหัสไปรษณีย์ (comb 5)
        DrawText(p1, font, d.Phone, 180.1, 196.0, 153.8, 12.8, XStringFormats.CenterLeft);        // f15 โทรศัพท์

        // ประกอบกิจการ (field 23) — ประเภทกิจการที่ประกอบ + รหัส ISIC (field 24, comb 6)
        DrawText(p1, font, d.BusinessActivity, 360.0, 356.0, 208.9, 14.0, XStringFormats.CenterLeft);
        DrawComb(p1, font, Digits(d.IsicCode), 498.5, 373.1, 70.7, 14.6, 6);

        // ผู้ตรวจสอบและรับรองบัญชี: เลขผู้เสียภาษี (field 43, comb 13) + ชื่อ (field 44) + เลขทะเบียน (field 45, comb 8)
        DrawDigitsAtCenters(p1, font, Digits(d.AuditorTaxId), AuditorTaxIdCellCenters, 732.5, 16.9);
        DrawText(p1, font, d.AuditorName, 214.8, 733.3, 254.4, 12.8, XStringFormats.CenterLeft);
        DrawComb(p1, font, Digits(d.AuditorLicenseNo), 472.2, 732.1, 94.3, 12.1, 8);

        // เลขผู้เสียภาษีสำนักงานสอบบัญชี (field 49, comb 13 — กริดฝั่งขวา)
        DrawDigitsAtCenters(p1, font, Digits(d.AuditFirmTaxId), FirmTaxIdCellCenters, 756.3, 16.9);
        // วันที่ในรายงานของผู้สอบบัญชี (field 46 วัน / 47 เดือน / 48 พ.ศ. — comb 2/2/4)
        if (d.AuditorSignDate is { } sd)
        {
            DrawComb(p1, font, sd.Day.ToString("00"), 232.3, 757.5, 23.6, 14.0, 2);
            DrawComb(p1, font, sd.Month.ToString("00"), 282.4, 757.5, 23.5, 14.0, 2);
            DrawComb(p1, font, (sd.Year + 543).ToString(), 329.0, 757.5, 47.3, 14.0, 4);
        }

        // ผู้ทำบัญชี: เลขผู้เสียภาษี (field 50, comb 13 — กริดเดียวกับ field 43) + ชื่อ (field 51)
        DrawDigitsAtCenters(p1, font, Digits(d.BookkeeperTaxId), AuditorTaxIdCellCenters, 800.0, 16.9);
        DrawText(p1, font, d.BookkeeperName, 217.7, 801.5, 175.7, 12.8, XStringFormats.CenterLeft);
        // เลขผู้เสียภาษีสำนักงานทำบัญชี = โปรไฟล์สำนักงาน (field 52, comb 13 — กริดฝั่งขวา)
        DrawDigitsAtCenters(p1, font, Digits(d.BookkeepingFirmTaxId), FirmTaxIdCellCenters, 800.5, 16.9);

        // ประเภทการยื่น: (1) ยื่นปกติ (Group1)
        DrawCheck(p1, font, 357.0, 166.0, 13.1, 13.7);
        // สถานะ: (1) บริษัท/ห้างฯ ตั้งตามกฎหมายไทย (Group2) — ค่าปกติ; ข้ามถ้าเป็นบริษัทมหาชน (สถานะอื่น)
        if (!(d.CompanyName?.Contains("มหาชน") ?? false))
            DrawCheck(p1, font, 47.0, 245.0, 13.7, 13.7);
        // รอบบัญชี ตั้งแต่ / ถึง (วัน/เดือน/ปี พ.ศ.) — ฟิลด์เป็น comb (วัน 2 / เดือน 2 / ปี 4 ช่อง)
        DrawComb(p1, font, d.PeriodStart.Day.ToString("00"), 400.4, 100.7, 22.6, 12.5, 2);
        DrawComb(p1, font, d.PeriodStart.Month.ToString("00"), 454.3, 100.7, 22.6, 12.5, 2);
        DrawComb(p1, font, (d.PeriodStart.Year + 543).ToString(), 511.2, 100.7, 45.7, 12.5, 4);
        DrawComb(p1, font, d.PeriodEnd.Day.ToString("00"), 400.4, 131.6, 22.6, 12.6, 2);
        DrawComb(p1, font, d.PeriodEnd.Month.ToString("00"), 454.3, 131.6, 22.6, 12.6, 2);
        DrawComb(p1, font, (d.PeriodEnd.Year + 543).ToString(), 511.2, 130.7, 45.7, 12.5, 4);

        // ── หน้า 2: การคำนวณภาษี (ขวา = จำนวนเงิน) ──
        DrawMoney(p2, font, d.NetTaxableIncome, 461.2, 244.3, 101.1, 19.7); // Text6 ฐานภาษี
        DrawMoney(p2, font, d.TaxAmount, 461.0, 316.8, 101.1, 19.7);        // Text7 ภาษีที่คำนวณได้
        DrawMoney(p2, font, d.WhtCredit, 327.8, 371.7, 101.1, 17.5);       // Text10 ภาษีหัก ณ ที่จ่าย
        DrawMoney(p2, font, d.TotalCredit, 461.9, 425.7, 101.1, 17.5);     // Text14 รวมรายการหัก
        DrawMoney(p2, font, Math.Abs(d.NetPayable), 461.8, 443.6, 101.1, 17.5); // Text15 คงเหลือ
        DrawMoney(p2, font, Math.Abs(d.NetPayable), 461.7, 479.6, 101.1, 17.5); // Text17 รวมสุทธิ

        // ── หน้า 2: ติ๊ก checkbox ตามผลคำนวณ ──
        // รายการ 2: เงินได้ที่ต้องเสียภาษี — (1) กำไรสุทธิ / (2) ขาดทุนสุทธิ (Group4)
        if (d.IsNetProfit)
            DrawCheck(p2, font, 32.8, 232.3, 13.1, 13.7);   // (1) กำไรสุทธิที่ต้องเสียภาษี
        else
            DrawCheck(p2, font, 171.6, 232.3, 13.2, 13.7);  // (2) ขาดทุนสุทธิ

        // การคำนวณภาษี: SME = ลดอัตราภาษี (Group5 ข้อ 1) + ประเภท (1.2) ทุนชำระแล้ว ≤ 5 ล้าน (Group6)
        if (d.RateScheme == TaxRateScheme.SmeTiered)
        {
            DrawCheck(p2, font, 32.8, 286.4, 13.7, 13.7);   // (1) กรณีลดอัตราภาษี
            DrawCheck(p2, font, 174.9, 286.4, 13.7, 14.2);  // (1.2) SME
        }

        // คงเหลือภาษี (Group7) + รวมภาษี (Group8): ชำระเพิ่มเติม (≥0) / ชำระไว้เกิน (<0)
        if (d.NetPayable >= 0)
        {
            DrawCheck(p2, font, 97.8, 449.3, 13.7, 13.1);   // คงเหลือ — ชำระเพิ่มเติม
            DrawCheck(p2, font, 97.8, 484.3, 13.2, 14.2);   // รวม — ชำระเพิ่มเติม
        }
        else
        {
            DrawCheck(p2, font, 171.6, 449.3, 13.2, 12.6);  // คงเหลือ — ชำระไว้เกิน
            DrawCheck(p2, font, 171.6, 484.3, 13.7, 13.7);  // รวม — ชำระไว้เกิน
        }

        // ── หน้า 3: รายการที่ 3 — reconciliation กำไรบัญชี → เงินได้สุทธิเพื่อเสียภาษี ──
        if (d.Page3 is { } p3d && doc.Pages.Count > 2)
        {
            var p3 = XGraphics.FromPdfPage(doc.Pages[2], XGraphicsPdfPageOptions.Append);
            // เติมคอลัมน์ 2 (กิจการที่ต้องเสียภาษี) + คอลัมน์ 3 (รวม) เท่ากัน; คอลัมน์ 1 (ยกเว้น) เว้น
            void Row(double y, decimal v)
            {
                DrawMoney(p3, font, v, 359.0, y, 101.2, 13.0);   // col2 เสียภาษี
                DrawMoney(p3, font, v, 466.9, y, 101.2, 13.0);   // col3 รวม
            }
            Row(97.6, p3d.Revenue);              // 1. รายได้โดยตรง
            Row(123.0, p3d.Cogs);                // 2. หัก ต้นทุนขาย
            Row(139.8, p3d.GrossProfit);         // 3. กำไร(ขาดทุน)ขั้นต้น
            Row(157.4, p3d.OtherIncome);         // 4. บวก รายได้อื่น
            Row(174.2, p3d.GrossProfit + p3d.OtherIncome);  // 5. รวม (3+4)
            Row(209.3, p3d.GrossProfit + p3d.OtherIncome);  // 7. รวม (5-6)
            Row(226.2, p3d.Sga);                 // 8. หัก รายจ่ายขายและบริหาร
            Row(243.7, p3d.NetAccountingProfit); // 9. กำไร(ขาดทุน)สุทธิตามบัญชี
            Row(288.2, p3d.AddBack);             // 11. บวก รายจ่ายต้องห้าม
            Row(305.6, p3d.NetAccountingProfit + p3d.AddBack); // 12. รวม (9+10+11)
            Row(330.9, p3d.Deduction);           // 13. หัก รายได้ยกเว้น/หักเพิ่ม
            Row(348.3, p3d.AdjustedProfit);      // 14. รวม (12-13)
            Row(364.9, p3d.LossUsed);            // 15. หัก ขาดทุนยกมา
            Row(382.3, p3d.NetTaxableIncome);    // 16. รวม (14-15)
            DrawMoney(p3, font, p3d.NetTaxableIncome, 466.1, 632.3, 101.1, 13.0); // 21. เงินได้สุทธิเพื่อเสียภาษี (col3)

            // checkbox กำไร/ขาดทุน
            if (p3d.GrossProfit >= 0) DrawCheck(p3, font, 38.3, 143.8, 12.0, 13.1);   // 3. กำไรขั้นต้น
            else DrawCheck(p3, font, 108.8, 143.8, 13.1, 13.1);                        // 3. ขาดทุนขั้นต้น
            if (p3d.NetAccountingProfit >= 0) DrawCheck(p3, font, 37.7, 247.1, 13.1, 12.6); // 9. กำไรสุทธิ
            else DrawCheck(p3, font, 108.8, 247.1, 13.1, 12.6);                        // 9. ขาดทุนสุทธิ
            if (p3d.NetTaxableIncome > 0) DrawCheck(p3, font, 38.3, 635.7, 12.6, 13.1); // 21. กำไรสุทธิที่ต้องเสียภาษี
            else DrawCheck(p3, font, 157.4, 635.2, 12.6, 14.2);                        // 21. ขาดทุนสุทธิ
        }

        // ── หน้า 7: รายการที่ 12 — งบดุล (crosswalk จากผังงบ) ──
        if (d.Page7 is { } p7 && doc.Pages.Count > 6)
        {
            var p7g = XGraphics.FromPdfPage(doc.Pages[6], XGraphicsPdfPageOptions.Append);
            void Bs(double y, decimal v) => DrawMoney(p7g, font, v, 459.1, y, 100.6, 14.0);
            Bs(80.4, p7.Cash);                 // 140 เงินสด
            Bs(98.2, p7.Ar);                   // 141 ลูกหนี้การค้า
            Bs(116.3, p7.Inventory);           // 142 สินค้าคงเหลือ
            Bs(134.2, p7.OtherCurrentAsset);   // 143 สินทรัพย์หมุนเวียนอื่น
            Bs(169.6, p7.LoansToRelated);      // 144 เงินให้กู้ยืมบุคคลที่เกี่ยวข้อง
            Bs(187.5, p7.Ppe);                 // 145 ที่ดิน อาคาร อุปกรณ์-สุทธิ
            Bs(205.5, p7.OtherAssetNet);       // 146 ทรัพย์สินอื่น-สุทธิ
            Bs(242.2, p7.OtherNonCurrentAsset);// 148 สินทรัพย์ไม่หมุนเวียนอื่น
            Bs(260.1, p7.TotalAssets);         // รวมสินทรัพย์
            Bs(312.8, p7.BankOdShortLoan);     // 149 เบิกเกินบัญชี+กู้ระยะสั้น
            Bs(330.7, p7.Ap);                  // 150 เจ้าหนี้การค้า
            Bs(348.8, p7.CurrentLoan);         // 151 เงินกู้ยืม
            Bs(366.7, p7.OtherCurrentLiab);    // 152 หนี้สินหมุนเวียนอื่น
            Bs(401.3, p7.LongTermLoan);        // 153 เงินกู้ยืมระยะยาว
            Bs(419.2, p7.OtherNonCurrentLiab); // 154 หนี้สินไม่หมุนเวียนอื่น
            Bs(437.9, p7.TotalLiabilities);    // รวมหนี้สิน
            Bs(472.8, p7.PaidUpCapital);       // 156 ทุนที่ออกและชำระแล้ว
            Bs(509.5, p7.RetainedEarnings);    // 158-159 กำไร/ขาดทุนสะสม
            Bs(529.0, p7.TotalEquity);         // 160 รวมส่วนของผู้ถือหุ้น
            Bs(546.2, p7.TotalLiabAndEquity);  // 161 รวมหนี้สิน+ทุน
            // checkbox กำไรสะสม (Group12)
            if (p7.IsRetainedProfit) DrawCheck(p7g, font, 44.3, 514.4, 13.1, 13.7);  // กำไรสะสม
            else DrawCheck(p7g, font, 128.5, 514.9, 13.7, 13.7);                      // ขาดทุนสะสม
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
                if (c.Amount != 0) DrawMoney(g, font, c.Amount, c.X, c.Y, c.W, 13.0);
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
        g.DrawString(text, f, XBrushes.Black, new XRect(x, y, w, h), fmt);
    }

    /// <summary>ติ๊กช่อง (วาดเครื่องหมายถูก ✓ แบบเส้น — ไม่พึ่ง glyph ฟอนต์)</summary>
    private static void DrawCheck(XGraphics g, XFont f, double x, double y, double w, double h)
    {
        var pen = new XPen(XColors.Black, Math.Max(1.0, h * 0.12));
        var pts = new[]
        {
            new XPoint(x + w * 0.20, y + h * 0.52),
            new XPoint(x + w * 0.42, y + h * 0.74),
            new XPoint(x + w * 0.82, y + h * 0.24),
        };
        g.DrawLines(pen, pts);
    }

    private static void DrawMoney(XGraphics g, XFont f, decimal v, double x, double y, double w, double h)
        => DrawText(g, f, v.ToString("#,##0.00", CultureInfo.InvariantCulture), x, y, w - 3, h, XStringFormats.CenterRight);

    /// <summary>วาดตัวเลขลงช่อง comb ที่กว้างเท่ากัน — กึ่งกลางแต่ละช่อง</summary>
    private static void DrawComb(XGraphics g, XFont f, string text, double x, double y, double w, double h, int cells)
    {
        if (string.IsNullOrEmpty(text)) return;
        var cellW = w / cells;
        for (int i = 0; i < text.Length && i < cells; i++)
            g.DrawString(text[i].ToString(), f, XBrushes.Black,
                new XRect(x + i * cellW, y, cellW, h), XStringFormats.Center);
    }

    /// <summary>วาดแต่ละหลักกึ่งกลางช่องตามพิกัด x ที่กำหนด (ช่องไม่เท่ากัน)</summary>
    private static void DrawDigitsAtCenters(XGraphics g, XFont f, string text, double[] centers, double y, double h)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length && i < centers.Length; i++)
            g.DrawString(text[i].ToString(), f, XBrushes.Black,
                new XRect(centers[i] - 5, y, 10, h), XStringFormats.Center);
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
