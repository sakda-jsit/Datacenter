using Datacenter.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Datacenter.Infrastructure.Services.Wht;

/// <summary>
/// ตัดขอบ/ช่องว่างรอบลายเซ็นด้วย ImageSharp — มองพิกเซลโปร่งใส (alpha ต่ำ) หรือพื้นขาว
/// (สว่างเกือบเต็ม) เป็น "ว่าง" แล้วครอปให้เหลือเฉพาะกรอบที่มีลายเซ็นจริง (เผื่อขอบเล็กน้อย).
/// </summary>
public class SignatureImageProcessor : ISignatureImageProcessor
{
    private const int AlphaThreshold = 24;   // ถือว่าโปร่งใสถ้า A <= ค่านี้
    private const int WhiteThreshold = 240;   // ถือว่าพื้นขาวถ้า R,G,B >= ค่านี้ (และทึบ)
    private const int Pad = 6;                 // เผื่อขอบรอบลายเซ็น (พิกเซล)

    public byte[] TrimWhitespace(byte[] image)
    {
        if (image is not { Length: > 0 }) return image;
        try
        {
            using var img = Image.Load<Rgba32>(image);
            int w = img.Width, h = img.Height;
            int minX = w, minY = h, maxX = -1, maxY = -1;

            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < h; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        var p = row[x];
                        bool blank = p.A <= AlphaThreshold ||
                                     (p.R >= WhiteThreshold && p.G >= WhiteThreshold && p.B >= WhiteThreshold);
                        if (blank) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            });

            // ไม่พบลายเซ็น (รูปว่าง) → คืนรูปเดิม
            if (maxX < minX || maxY < minY) return image;

            int x0 = Math.Max(0, minX - Pad);
            int y0 = Math.Max(0, minY - Pad);
            int x1 = Math.Min(w - 1, maxX + Pad);
            int y1 = Math.Min(h - 1, maxY + Pad);
            var rect = new Rectangle(x0, y0, x1 - x0 + 1, y1 - y0 + 1);

            // ครอบเต็มอยู่แล้ว → ไม่ต้องครอป (เลี่ยงงานเปล่า)
            if (rect.X == 0 && rect.Y == 0 && rect.Width == w && rect.Height == h)
                return image;

            img.Mutate(ctx => ctx.Crop(rect));
            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch
        {
            return image; // ประมวลผลไม่ได้ → ใช้รูปเดิม
        }
    }
}
