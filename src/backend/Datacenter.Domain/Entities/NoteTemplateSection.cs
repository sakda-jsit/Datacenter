using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ข้อความบรรยาย (narrative) ของหมายเหตุประกอบงบการเงิน (NOTE2) ที่เจ้าหน้าที่บัญชี "แก้ไขได้"
/// เมื่อมาตรฐานการบัญชีเปลี่ยน (req v11 docs/13 §5: Template text/form มี EffectiveYear).
///
/// แยกจากส่วน data-binding (ตัวเลขดึงจาก TB อัตโนมัติ ห้ามแก้ตรง) อย่างชัดเจน —
/// entity นี้เก็บเฉพาะ "ข้อความ" ไม่เก็บตัวเลข.
///
/// การเลือกใช้ตอน render: เลือก section ที่ EffectiveYear ≤ ปีงบ ที่ใกล้ที่สุด ต่อ (Company, NoteKey).
/// ClientCompanyId = null คือ template กลาง (default) ใช้ได้ทุกบริษัท; ถ้าบริษัทมีของตัวเองให้ทับ.
///
/// BodyText รองรับ placeholder ที่ระบบแทนค่าตอน render:
///   {{CompanyName}} {{TaxId}} {{Address}} {{FiscalYear}} {{FiscalYearTh}} {{PriorYear}} {{PriorYearTh}}
/// แต่ละย่อหน้าคั่นด้วยขึ้นบรรทัดใหม่ (\n).
/// </summary>
public class NoteTemplateSection : BaseEntity
{
    /// <summary>null = template กลาง (default ทุกบริษัท); มีค่า = เฉพาะบริษัทนั้น (ทับ default)</summary>
    public int? ClientCompanyId { get; set; }

    /// <summary>ปีที่เริ่มมีผล (พ.ศ.) — เลือกตัวที่ ≤ ปีงบ ที่ใกล้ที่สุด</summary>
    public int EffectiveYear { get; set; }

    /// <summary>รหัสหมายเหตุ เช่น "1","2","3","4","5","6.5","6.11","6.12","7"</summary>
    public string NoteKey { get; set; } = string.Empty;

    /// <summary>หัวข้อหมายเหตุ</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>เนื้อความ (หลายย่อหน้าคั่นด้วย \n) รองรับ placeholder {{...}}</summary>
    public string BodyText { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ClientCompany? ClientCompany { get; set; }
}
