using System.Globalization;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.CorporateTax.DTOs;
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
        // เลขประจำตัวผู้เสียภาษี (comb 13 ช่องเท่ากัน)
        DrawComb(p1, font, Digits(d.TaxId), 148.6, 88.6, 180.4, 16.9, 13);
        // ชื่อบริษัท
        DrawText(p1, font, d.CompanyName, 42, 115.9, 291, 12.6, XStringFormats.CenterLeft);
        // รอบบัญชี ตั้งแต่ / ถึง (วัน/เดือน/ปี พ.ศ.)
        DrawText(p1, font, d.PeriodStart.Day.ToString("00"), 400.4, 100.7, 22.6, 12.5, XStringFormats.Center);
        DrawText(p1, font, d.PeriodStart.Month.ToString("00"), 454.3, 100.7, 22.6, 12.5, XStringFormats.Center);
        DrawText(p1, font, (d.PeriodStart.Year + 543).ToString(), 511.2, 100.7, 45.7, 12.5, XStringFormats.Center);
        DrawText(p1, font, d.PeriodEnd.Day.ToString("00"), 400.4, 131.6, 22.6, 12.6, XStringFormats.Center);
        DrawText(p1, font, d.PeriodEnd.Month.ToString("00"), 454.3, 131.6, 22.6, 12.6, XStringFormats.Center);
        DrawText(p1, font, (d.PeriodEnd.Year + 543).ToString(), 511.2, 130.7, 45.7, 12.5, XStringFormats.Center);

        // ── หน้า 2: การคำนวณภาษี (ขวา = จำนวนเงิน) ──
        DrawMoney(p2, font, d.NetTaxableIncome, 461.2, 244.3, 101.1, 19.7); // Text6 ฐานภาษี
        DrawMoney(p2, font, d.TaxAmount, 461.0, 316.8, 101.1, 19.7);        // Text7 ภาษีที่คำนวณได้
        DrawMoney(p2, font, d.WhtCredit, 327.8, 371.7, 101.1, 17.5);       // Text10 ภาษีหัก ณ ที่จ่าย
        DrawMoney(p2, font, d.TotalCredit, 461.9, 425.7, 101.1, 17.5);     // Text14 รวมรายการหัก
        DrawMoney(p2, font, Math.Abs(d.NetPayable), 461.8, 443.6, 101.1, 17.5); // Text15 คงเหลือ
        DrawMoney(p2, font, Math.Abs(d.NetPayable), 461.7, 479.6, 101.1, 17.5); // Text17 รวมสุทธิ

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

    private static void DrawMoney(XGraphics g, XFont f, decimal v, double x, double y, double w, double h)
        => DrawText(g, f, v.ToString("#,##0.00", CultureInfo.InvariantCulture), x, y, w - 3, h, XStringFormats.CenterRight);

    /// <summary>วาดตัวเลขลงช่อง comb (ช่องกว้างเท่ากัน) — กึ่งกลางแต่ละช่อง</summary>
    private static void DrawComb(XGraphics g, XFont f, string text, double x, double y, double w, double h, int cells)
    {
        if (string.IsNullOrEmpty(text)) return;
        var cellW = w / cells;
        for (int i = 0; i < text.Length && i < cells; i++)
            g.DrawString(text[i].ToString(), f, XBrushes.Black,
                new XRect(x + i * cellW, y, cellW, h), XStringFormats.Center);
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
