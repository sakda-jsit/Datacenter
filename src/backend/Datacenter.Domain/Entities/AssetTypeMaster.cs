using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// มาสเตอร์ประเภทสินทรัพย์ + อัตรา/อายุค่าเสื่อมมาตรฐาน (req v11 docs/14 คำตอบ #4).
/// เป็นมาสเตอร์ระดับระบบ (ไม่ผูกบริษัท) — seed ค่ามาตรฐานตอน startup.
/// สร้างสินทรัพย์จะ default อัตราจากที่นี่ แล้ว override รายตัวได้.
/// </summary>
public class AssetTypeMaster : BaseEntity
{
    /// <summary>รหัสประเภท เช่น BUILDING, VEHICLE, EQUIPMENT</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>ชื่อประเภท (ไทย)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>อัตราค่าเสื่อมชุดบัญชี (% ต่อปี, เส้นตรง)</summary>
    public decimal DefaultBookRatePct { get; set; }

    /// <summary>อัตราค่าเสื่อมชุดภาษี (% ต่อปี, เส้นตรง)</summary>
    public decimal DefaultTaxRatePct { get; set; }

    /// <summary>อายุการใช้งานอ้างอิง (ปี) — เชิงข้อมูล</summary>
    public int DefaultUsefulLifeYears { get; set; }

    public bool IsActive { get; set; } = true;
}
