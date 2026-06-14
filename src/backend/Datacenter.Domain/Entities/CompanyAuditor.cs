using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// บันทึกผู้ลงนามรับผิดชอบงบ "เฉพาะรอบปี" (บริษัท, ปีงบ) — ใช้เมื่อปีนั้น "ต่างจากค่าเริ่มต้นของบริษัท"
/// หรือเก็บวันที่ในรายงานผู้สอบของปีนั้น. ถ้าปีไหนไม่มี override → ใช้ผู้ลงนามประจำบริษัท
/// (ClientCompany.DefaultAuditorId / DefaultBookkeeperId).
/// AuditorId/BookkeeperId อ้างอิงทะเบียน master (Auditor/Bookkeeper). ป้อนมือ — แก้ไข/ลบได้ + audit.
/// </summary>
public class CompanyAuditor : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ปีบัญชี (ค.ศ.)</summary>
    public int FiscalYear { get; set; }

    /// <summary>ผู้สอบบัญชีเฉพาะปีนี้ (override; null = ใช้ค่าเริ่มต้นบริษัท) → ทะเบียน Auditor</summary>
    public int? AuditorId { get; set; }
    public Auditor? Auditor { get; set; }

    /// <summary>ผู้ทำบัญชีเฉพาะปีนี้ (override; null = ใช้ค่าเริ่มต้นบริษัท) → ทะเบียน Bookkeeper</summary>
    public int? BookkeeperId { get; set; }
    public Bookkeeper? Bookkeeper { get; set; }

    /// <summary>วันที่ในรายงานของผู้สอบบัญชี (รอบปีนี้)</summary>
    public DateTime? SignDate { get; set; }

    public string? Note { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;

    // ── Legacy (เลิกใช้): free-text เดิมก่อนแยกเป็นทะเบียน master — เก็บไว้ backfill แล้วค่อยลบภายหลัง ──
    public string? AuditorName { get; set; }
    public string? AuditorLicenseNo { get; set; }
    public string? AuditorTaxId { get; set; }
    public string? BookkeeperName { get; set; }
    public string? BookkeeperTaxId { get; set; }
    public string? AuditFirmName { get; set; }
    public string? AuditFirmTaxId { get; set; }
}
