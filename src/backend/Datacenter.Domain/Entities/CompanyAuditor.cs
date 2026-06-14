using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ผู้ลงนามรับผิดชอบงบการเงินของบริษัท "ต่อรอบปีบัญชี" — ผูกกับ (บริษัท, ปีงบ):
/// ผู้ตรวจสอบและรับรองบัญชี (ผู้สอบบัญชี) + ผู้ทำบัญชี.
/// แยกจาก ClientCompany เพราะทั้งสองเปลี่ยนได้รายปี (ปีที่แล้วกับปีนี้อาจไม่ใช่คนเดิม).
/// ใช้เติมส่วน "ผู้ตรวจสอบและรับรองบัญชี" + "ผู้ทำบัญชี" ในแบบ ภ.ง.ด.50 ตามปีที่ยื่นจริง.
/// ป้อนมือ (Express ไม่มีข้อมูลนี้) — แก้ไข/ลบได้ + audit.
/// </summary>
public class CompanyAuditor : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ปีบัญชี (ค.ศ.)</summary>
    public int FiscalYear { get; set; }

    /// <summary>ชื่อผู้ตรวจสอบและรับรองบัญชี</summary>
    public string AuditorName { get; set; } = string.Empty;

    /// <summary>เลขทะเบียนผู้สอบบัญชี (CPA/TA)</summary>
    public string? AuditorLicenseNo { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของผู้สอบบัญชี</summary>
    public string? AuditorTaxId { get; set; }

    /// <summary>ชื่อผู้ทำบัญชี (เปลี่ยนได้รายปีเช่นกัน)</summary>
    public string? BookkeeperName { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของผู้ทำบัญชี</summary>
    public string? BookkeeperTaxId { get; set; }

    /// <summary>ชื่อสำนักงานสอบบัญชี (สังกัดของผู้สอบบัญชี — อาจต่างกันแต่ละบริษัท/ปี)</summary>
    public string? AuditFirmName { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของสำนักงานสอบบัญชี</summary>
    public string? AuditFirmTaxId { get; set; }

    /// <summary>วันที่ลงนามรับรองงบการเงิน</summary>
    public DateTime? SignDate { get; set; }

    public string? Note { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
