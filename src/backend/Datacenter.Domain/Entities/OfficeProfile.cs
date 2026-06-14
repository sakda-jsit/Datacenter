using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// โปรไฟล์ "สำนักงานบัญชี" ของผู้ใช้ระบบ — ค่ากลางของระบบ (singleton, ไม่แยกบริษัทลูกค้า).
/// ใช้เติม "สำนักงานทำบัญชี" (เลขประจำตัวผู้เสียภาษี) ในแบบ ภ.ง.ด.50 ให้ทุกบริษัทอัตโนมัติ
/// และเป็นข้อมูลสำนักงานสำหรับเอกสาร/หัวรายงานอื่น ๆ. แก้ไขในเมนูตั้งค่ากลาง + audit.
/// </summary>
public class OfficeProfile : BaseEntity
{
    /// <summary>ชื่อสำนักงานบัญชี (นิติบุคคล)</summary>
    public string OfficeName { get; set; } = string.Empty;

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของสำนักงาน — ใช้เป็น "สำนักงานทำบัญชี" ในแบบภาษี</summary>
    public string? TaxId { get; set; }

    public string? BranchCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
