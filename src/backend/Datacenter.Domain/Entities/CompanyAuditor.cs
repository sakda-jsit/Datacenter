using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ผู้ตรวจสอบและรับรองบัญชี (ผู้สอบบัญชี) ของบริษัท "ต่อรอบปีบัญชี" — ผูกกับ (บริษัท, ปีงบ).
/// แยกจาก ClientCompany เพราะผู้สอบบัญชีเปลี่ยนได้รายปี (ปีที่แล้วกับปีนี้อาจไม่ใช่คนเดิม).
/// ใช้เติมส่วน "ผู้ตรวจสอบและรับรองบัญชี" ในแบบ ภ.ง.ด.50 และเอกสารงบการเงินตามปีที่ยื่นจริง.
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

    /// <summary>วันที่ลงนามรับรองงบการเงิน</summary>
    public DateTime? SignDate { get; set; }

    public string? Note { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
