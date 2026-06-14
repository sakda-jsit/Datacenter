using System.Text.RegularExpressions;

namespace Datacenter.Application.Features.CorporateTax.Services;

/// <summary>
/// แยกที่อยู่ไทยแบบ flat (จาก Express ISINFO เช่น "39/6 หมู่12 ต.นาป่า อ.เมืองชลบุรี จ.ชลบุรี 20000")
/// → ส่วนประกอบสำหรับเติมช่องในแบบ ภ.ง.ด.50. ถ้ารูปแบบไม่ตรง ช่องที่แยกไม่ได้จะเว้นว่าง (graceful).
/// </summary>
public static class ThaiAddressParser
{
    public record Parts(
        string? HouseNo, string? Moo, string? Soi, string? Road,
        string? SubDistrict, string? District, string? Province, string? PostalCode);

    public static Parts Parse(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return new Parts(null, null, null, null, null, null, null, null);

        var s = address.Trim();

        // รหัสไปรษณีย์ = เลข 5 หลักท้ายสุด
        string? postal = null;
        var pm = Regex.Match(s, @"(\d{5})\s*$");
        if (pm.Success) { postal = pm.Groups[1].Value; s = s[..pm.Index].Trim(); }

        string? Grab(params string[] prefixes)
        {
            foreach (var p in prefixes)
            {
                // จับ "ต.นาป่า" / "ตำบลนาป่า" / "ต. นาป่า" → ค่าจนถึงช่องว่างถัดไป
                var m = Regex.Match(s, Regex.Escape(p) + @"\s*([^\s]+)");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            return null;
        }

        var moo = Grab("หมู่ที่", "หมู่", "ม.");
        var soi = Grab("ซอย", "ซ.");
        var road = Grab("ถนน", "ถ.");
        var sub = Grab("ตำบล", "ต.", "แขวง");
        var dist = Grab("อำเภอ", "อ.", "เขต");
        var prov = Grab("จังหวัด", "จ.");

        // เลขที่ = token แรกสุดก่อนคำนำหน้าใด ๆ (เช่น "39/6")
        string? house = null;
        var first = Regex.Match(s, @"^\s*([^\s]+)");
        if (first.Success)
        {
            var tok = first.Groups[1].Value;
            if (!Regex.IsMatch(tok, @"^(หมู่|ม\.|ซอย|ซ\.|ถนน|ถ\.|ตำบล|ต\.|แขวง|อำเภอ|อ\.|เขต|จังหวัด|จ\.)"))
                house = tok;
        }

        return new Parts(house, moo, soi, road, sub, dist, prov, postal);
    }
}
