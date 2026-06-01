using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// Stores values that cannot be derived from the trial balance.
/// Primary use: X4 (corporate income tax) which comes from ภงด.50, not from accounting entries.
/// One row per (ClientCompanyId, FiscalYear, RefCode).
/// </summary>
public class FsExternalInput : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public int FiscalYear { get; set; }
    /// <summary>REF code this amount applies to. Currently only "X4".</summary>
    public string RefCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
