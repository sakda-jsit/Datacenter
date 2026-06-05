using Datacenter.Domain.Entities;

namespace Datacenter.Infrastructure.Persistence.Seeds;

/// <summary>
/// มาสเตอร์ประเภทสินทรัพย์มาตรฐาน + อัตราค่าเสื่อมเริ่มต้น (req v11 docs/14 คำตอบ #4).
/// อัตราชุดบัญชี/ภาษีอิงแนวปฏิบัติเส้นตรงทั่วไปและเพดานตามประมวลรัษฎากร — override รายตัวได้.
/// </summary>
public static class AssetTypeSeed
{
    public static IEnumerable<AssetTypeMaster> GetTypes() =>
    [
        New("LAND",      "ที่ดิน",                          0m,   0m,  0),
        New("BUILDING",  "อาคารและสิ่งปลูกสร้าง",            5m,   5m, 20),
        New("MACHINE",   "เครื่องจักรและอุปกรณ์โรงงาน",      10m,  20m, 10),
        New("EQUIPMENT", "เครื่องใช้สำนักงาน/อุปกรณ์",       20m,  20m,  5),
        New("FURNITURE", "เครื่องตกแต่งและเครื่องใช้สำนักงาน", 20m,  20m,  5),
        New("COMPUTER",  "คอมพิวเตอร์และอุปกรณ์",            33.33m, 33.33m, 3),
        New("VEHICLE",   "ยานพาหนะ",                         20m,  20m,  5),
        New("TOOLS",     "เครื่องมือและอุปกรณ์",             20m,  20m,  5),
    ];

    private static AssetTypeMaster New(string code, string name, decimal book, decimal tax, int life)
        => new()
        {
            Code = code,
            Name = name,
            DefaultBookRatePct = book,
            DefaultTaxRatePct = tax,
            DefaultUsefulLifeYears = life,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
        };
}
