using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ลูกค้า (ลูกหนี้การค้า) — นำเข้าจาก Express ARMAS.DBF (master, upsert by รหัสลูกค้า).
/// </summary>
public class Customer : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสลูกค้า (ARMAS.CUSCOD) — business key</summary>
    public string CustomerCode { get; set; } = string.Empty;

    public string? Prefix { get; set; }              // PRENAM
    public string Name { get; set; } = string.Empty; // CUSNAM
    public string? TaxId { get; set; }               // TAXID
    public string? Address { get; set; }             // ADDR01-03 + ZIP รวม
    public string? Phone { get; set; }               // TELNUM
    public string? Contact { get; set; }             // CONTACT
    public string? Email { get; set; }               // แยกจาก REMARK ถ้ามี "E-MAIL:"

    /// <summary>เครดิตเทอม (วัน) — ARMAS.PAYTRM</summary>
    public int PaymentTermDays { get; set; }
    public string? PaymentCondition { get; set; }    // PAYCOND
    public string? GlAccountCode { get; set; }       // ACCNUM (บัญชีลูกหนี้)
    public string? Remark { get; set; }              // REMARK
    public bool IsActive { get; set; } = true;       // STATUS='A'

    public ClientCompany ClientCompany { get; set; } = null!;
}
