using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ผู้ขาย (เจ้าหนี้การค้า) — นำเข้าจาก Express APMAS.DBF (master, upsert by รหัสผู้ขาย).
/// </summary>
public class Supplier : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสผู้ขาย (APMAS.SUPCOD) — business key</summary>
    public string SupplierCode { get; set; } = string.Empty;

    public string? Prefix { get; set; }              // PRENAM
    public string Name { get; set; } = string.Empty; // SUPNAM
    public string? TaxId { get; set; }               // TAXID
    public string? Address { get; set; }             // ADDR01-03 + ZIP
    public string? Phone { get; set; }               // TELNUM
    public string? Contact { get; set; }             // CONTACT
    public string? Email { get; set; }               // จาก REMARK ถ้ามี

    public int PaymentTermDays { get; set; }         // PAYTRM
    public string? PaymentCondition { get; set; }    // PAYCOND
    public string? GlAccountCode { get; set; }       // ACCNUM (บัญชีเจ้าหนี้)
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;       // STATUS != '0'

    public ClientCompany ClientCompany { get; set; } = null!;
}
