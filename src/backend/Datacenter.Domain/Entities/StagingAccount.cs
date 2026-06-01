namespace Datacenter.Domain.Entities;

/// <summary>
/// Staging table for chart of accounts rows imported from Express DBF (GLACC).
/// Validated then promoted to Account table.
/// </summary>
public class StagingAccount
{
    public long Id { get; set; }
    public int ImportBatchId { get; set; }
    public int ClientCompanyId { get; set; }

    // GLACC field mappings
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? AccountName2 { get; set; }
    public int Level { get; set; }
    public string? ParentCode { get; set; }
    // 1=Assets 2=Liabilities 3=Equity 4=Income 5=Expenses
    public int Group { get; set; }
    // 0=detail 1=header
    public int AccountType { get; set; }

    public bool IsValid { get; set; } = true;
    public string? ValidationError { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
}
