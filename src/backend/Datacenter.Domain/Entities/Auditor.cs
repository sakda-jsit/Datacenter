using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ทะเบียนผู้ตรวจสอบและรับรองบัญชี (ผู้สอบบัญชี) — master ของสำนักงาน ใช้ซ้ำได้หลายบริษัท/ปี.
/// รวมข้อมูลสำนักงานสอบบัญชี (สังกัด) ไว้ในตัว. ป้อนมือ — แก้ไข/ลบได้ + audit.
/// </summary>
public class Auditor : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>ประเภททะเบียน (CPA = สอบได้ทุกนิติบุคคล / TA = เฉพาะ หจก. ขนาดเล็ก)</summary>
    public AuditorType Type { get; set; } = AuditorType.Cpa;

    /// <summary>เลขทะเบียนผู้สอบบัญชี (CPA/TA)</summary>
    public string? LicenseNo { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของผู้สอบบัญชี</summary>
    public string? TaxId { get; set; }

    /// <summary>ชื่อสำนักงานสอบบัญชี (สังกัดของผู้สอบ)</summary>
    public string? AuditFirmName { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของสำนักงานสอบบัญชี</summary>
    public string? AuditFirmTaxId { get; set; }

    public bool IsActive { get; set; } = true;
}
