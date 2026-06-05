using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// บัญชีเงินฝากธนาคาร — นำเข้าจาก Express BKMAS.DBF (master, upsert by รหัสบัญชีธนาคาร).
/// </summary>
public class BankAccount : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสบัญชีธนาคารใน Express (BKMAS.BNKACC) — business key</summary>
    public string BankAccountCode { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;   // BNKNAM
    public string? Branch { get; set; }                     // BRANCH
    public string? ShortName { get; set; }                  // SHORTNAM
    public string? AccountNumber { get; set; }              // BNKNUM (เลขที่บัญชี)
    public string? GlAccountCode { get; set; }              // ACCNUM (บัญชี GL เงินฝาก)

    /// <summary>ยอดยกมา (BKMAS.BALFWD ณ BALDAT)</summary>
    public decimal BalanceForward { get; set; }
    public DateTime? BalanceDate { get; set; }              // BALDAT

    public bool IsActive { get; set; } = true;

    public ClientCompany ClientCompany { get; set; } = null!;
}
