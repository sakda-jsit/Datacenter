using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// แมพรหัสแผนก/สาขาใน Express (ISVAT.DEPCOD) → เลขสาขาตามแบบ ภ.พ.30 ของกรมสรรพากร ต่อบริษัท.
/// ใช้ตอนแตกยอด ภ.พ.30 รายสาขา (ยื่นรวมกัน) — ถ้ามี mapping จะ override กฎแปลงอัตโนมัติ
/// (ว่าง/HO* → 00000, BR01 → 00001). ป้อน/แก้ได้ทุกคนที่มีสิทธิ์ + audit.
/// </summary>
public class VatBranchMapping : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัส DEPCOD ดิบจาก Express ("" = ไม่ระบุแผนก)</summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>เลขสาขาตามแบบ RD (สำนักงานใหญ่ = 00000, สาขา = 00001…)</summary>
    public string RdBranchNo { get; set; } = "00000";

    public bool IsHeadOffice { get; set; }

    /// <summary>ชื่อสาขา (ไม่บังคับ — ไว้ดูอ้างอิง)</summary>
    public string? BranchName { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
