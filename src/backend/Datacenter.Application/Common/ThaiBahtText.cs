using System.Globalization;

namespace Datacenter.Application.Common;

/// <summary>
/// แปลงจำนวนเงินเป็นข้อความภาษาไทย เช่น 585.47 → "ห้าร้อยแปดสิบห้าบาทสี่สิบเจ็ดสตางค์".
/// ใช้พิมพ์ในหนังสือรับรองหัก ณ ที่จ่าย (50 ทวิ).
/// </summary>
public static class ThaiBahtText
{
    private static readonly string[] Digits = { "ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า" };
    private static readonly string[] Places = { "", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน" };

    public static string Convert(decimal amount)
    {
        bool negative = amount < 0;
        amount = Math.Abs(Math.Round(amount, 2, MidpointRounding.AwayFromZero));

        long baht = (long)Math.Floor(amount);
        int satang = (int)Math.Round((amount - baht) * 100m, MidpointRounding.AwayFromZero);

        var sb = new System.Text.StringBuilder();
        if (negative) sb.Append("ลบ");

        sb.Append(baht == 0 ? "ศูนย์บาท" : ReadInteger(baht) + "บาท");
        sb.Append(satang == 0 ? "ถ้วน" : ReadInteger(satang) + "สตางค์");

        return sb.ToString();
    }

    /// <summary>อ่านจำนวนเต็มเป็นข้อความไทย รองรับหลัก "ล้าน" แบบวนซ้ำ</summary>
    private static string ReadInteger(long n)
    {
        if (n == 0) return Digits[0];

        // แยกกลุ่มละ 6 หลัก (ต่อด้วย "ล้าน")
        if (n >= 1_000_000)
        {
            long high = n / 1_000_000;
            long low = n % 1_000_000;
            var result = ReadInteger(high) + "ล้าน";
            if (low > 0) result += ReadBelowMillion((int)low);
            return result;
        }

        return ReadBelowMillion((int)n);
    }

    /// <summary>อ่านเลข 1..999,999</summary>
    private static string ReadBelowMillion(int n)
    {
        var s = n.ToString(CultureInfo.InvariantCulture);
        var sb = new System.Text.StringBuilder();
        int len = s.Length;

        for (int i = 0; i < len; i++)
        {
            int digit = s[i] - '0';
            int place = len - i - 1; // 0=หน่วย, 1=สิบ, ...
            if (digit == 0) continue;

            if (place == 0 && digit == 1 && len > 1)
                sb.Append("เอ็ด");                       // ...เอ็ด (เช่น สิบเอ็ด, ยี่สิบเอ็ด)
            else if (place == 1 && digit == 2)
                sb.Append("ยี่");                        // ยี่สิบ
            else if (place == 1 && digit == 1)
                { /* สิบ — ไม่อ่าน "หนึ่ง" */ }
            else
                sb.Append(Digits[digit]);

            sb.Append(Places[place]);
        }

        return sb.ToString();
    }
}
