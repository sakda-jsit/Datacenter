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

        // (1) ยื่นปกติ (Group1 first)
        DrawCheck(p1, font, 357.0, 158.3, 13.2, 13.6);
        // รอบบัญชี ตั้งแต่/ถึง (comb 2/2/4)
        DrawComb(p1, font, d.PeriodStart.Day.ToString("00"), 399.8, 97.9, 24.0, 12.5, 2);
        DrawComb(p1, font, d.PeriodStart.Month.ToString("00"), 453.9, 97.4, 24.9, 12.5, 2);
        DrawComb(p1, font, (d.PeriodStart.Year + 543).ToString(), 511.1, 96.9, 46.7, 12.5, 4);
        DrawComb(p1, font, d.PeriodEnd.Day.ToString("00"), 399.6, 127.6, 23.5, 12.5, 2);
        DrawComb(p1, font, d.PeriodEnd.Month.ToString("00"), 453.3, 128.5, 24.4, 12.5, 2);
        DrawComb(p1, font, (d.PeriodEnd.Year + 543).ToString(), 511.3, 127.5, 47.1, 12.5, 4);

        // ── หน้า 2: การคำนวณภาษี (ขวา = จำนวนเงิน) ──
        // ── หน้า 2: รายการที่ 1 การคำนวณภาษี (ฟอร์มใหม่ 2568) ── ฐานภาษีไปอยู่หน้า 3 (บรรทัด 21)
        DrawMoney(p2, font, d.TaxAmount, 461.7, 458.3, 105.8, 16.5);        // f50 ภาษีที่คำนวณได้
        DrawMoney(p2, font, d.WhtCredit, 327.6, 524.3, 105.5, 16.5);       // f54 ภาษีหัก ณ ที่จ่าย
        DrawMoney(p2, font, d.TotalCredit, 461.3, 590.5, 106.5, 16.5);     // f57รวม รวมรายการหัก
        DrawMoney(p2, font, Math.Abs(d.NetPayable), 461.3, 612.6, 106.5, 16.5); // f58 คงเหลือ
        DrawMoney(p2, font, Math.Abs(d.NetPayable), 461.7, 656.0, 106.5, 16.5); // f61 รวม

        // checkbox: กำไร/ขาดทุนสุทธิ (Group5)
        if (d.IsNetProfit) DrawCheck(p2, font, 34.0, 330.2, 12.0, 11.0);
        else DrawCheck(p2, font, 173.2, 330.9, 12.0, 11.0);
        // การคำนวณภาษี: SME → (2) ลดอัตราภาษี (Group21) + SMEs (Group6); อื่น → (1) กรณีทั่วไป
        if (d.RateScheme == TaxRateScheme.SmeTiered)
        {
            DrawCheck(p2, font, 32.8, 419.6, 12.1, 12.6);   // (2) กรณีลดอัตราภาษี
            DrawCheck(p2, font, 136.6, 418.6, 11.5, 12.0);  // SMEs
        }
        else DrawCheck(p2, font, 34.0, 396.4, 11.2, 12.6);  // (1) กรณีทั่วไป
        // คงเหลือ (Group7) + รวม (Group8): ชำระเพิ่มเติม (≥0) / ชำระไว้เกิน (<0)
        if (d.NetPayable >= 0)
        {
            DrawCheck(p2, font, 99.0, 616.4, 10.5, 12.2);   // คงเหลือ ชำระเพิ่มเติม
            DrawCheck(p2, font, 98.4, 659.7, 11.8, 11.5);   // รวม ชำระเพิ่มเติม
        }
        else
        {
            DrawCheck(p2, font, 172.8, 616.8, 10.9, 11.2);  // คงเหลือ ชำระไว้เกิน
            DrawCheck(p2, font, 173.6, 659.6, 10.5, 11.4);  // รวม ชำระไว้เกิน
        }

        // ── หน้า 3: รายการที่ 3 — reconciliation กำไรบัญชี → เงินได้สุทธิเพื่อเสียภาษี ──
        if (d.Page3 is { } p3d && doc.Pages.Count > 2)
        {
            var p3 = XGraphics.FromPdfPage(doc.Pages[2], XGraphicsPdfPageOptions.Append);
            // เติมคอลัมน์ 2 (กิจการที่ต้องเสียภาษี) + คอลัมน์ 3 (รวม) เท่ากัน; คอลัมน์ 1 (ยกเว้น) เว้น
            void Row(double y, decimal v)
            {
                DrawMoney(p3, font, v, 357.0, y, 109.0, 13.0);   // col2 เสียภาษี
                DrawMoney(p3, font, v, 466.9, y, 101.2, 13.0);   // col3 รวม
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
            DrawMoney(p3, font, p3d.NetTaxableIncome, 466.9, 594.8, 101.2, 13.0); // 21. เงินได้สุทธิเพื่อเสียภาษี (col3)

            // checkbox กำไร/ขาดทุน (Group100 L3 / Group101 L9 / Group9 L21)
            if (p3d.GrossProfit >= 0) DrawCheck(p3, font, 36.5, 180.7, 12.6, 13.0);
            else DrawCheck(p3, font, 107.1, 181.2, 13.1, 13.0);
            if (p3d.NetAccountingProfit >= 0) DrawCheck(p3, font, 36.6, 297.8, 13.0, 12.6);
            else DrawCheck(p3, font, 106.9, 297.4, 13.0, 12.6);
            if (p3d.NetTaxableIncome > 0) DrawCheck(p3, font, 36.9, 598.3, 12.6, 13.0);
            else DrawCheck(p3, font, 144.5, 597.6, 12.6, 13.0);
        }

        // ── หน้า 6 (index 5): รายการที่ 9 — งบดุล (ฟอร์มใหม่ 2568, crosswalk จากผังงบ) ──
        if (d.Page7 is { } p7 && doc.Pages.Count > 5)
        {
            var p7g = XGraphics.FromPdfPage(doc.Pages[5], XGraphicsPdfPageOptions.Append);
            void Bs(double y, decimal v) => DrawMoney(p7g, font, v, 456.6, y, 100.6, 14.0);
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
            // checkbox กำไร/ขาดทุนสะสม (Group91)
            if (p7.IsRetainedProfit) DrawCheck(p7g, font, 41.7, 503.9, 10.5, 10.5);
            else DrawCheck(p7g, font, 127.8, 503.9, 10.5, 10.5);
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
