using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

public class ClientCompany : BaseEntity
{
    /// <summary>รหัส dataset ใน Express (เช่น JSIT2016) — ใช้หาโฟลเดอร์ DBF, ไม่แสดงในระบบ</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>ชื่อจาก Express (THINAM) — sync ทับได้ทุกครั้งที่ import เป็นค่าอ้างอิง</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ชื่อทางการสำหรับออกงบ/แสดงทั้งระบบ. import/seed ครั้งแรก default = ชื่อ Express
    /// แล้วแก้ไขได้เอง; import ครั้งถัดไป **ไม่ทับ** ค่านี้ (กันชื่อที่แก้แล้วหาย).
    /// (Historical: เมื่อทำ report package จะ snapshot ค่านี้ต่อปี/version — docs/18)
    /// </summary>
    public string LegalName { get; set; } = string.Empty;

    /// <summary>ชื่ออังกฤษจาก Express (ENGNAM) — sync ได้</summary>
    public string? EnglishName { get; set; }

    public string TaxId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = "00000";
    public string? Address { get; set; }
    public int FiscalYearStartMonth { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<CompanyUserAccess> UserAccesses { get; set; } = [];
}
