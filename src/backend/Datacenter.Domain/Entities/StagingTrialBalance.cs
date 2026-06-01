namespace Datacenter.Domain.Entities;

/// <summary>
/// Staging table for trial balance rows imported from Express DBF (GLBAL).
/// Validated then promoted to JournalEntry/Account tables.
/// </summary>
public class StagingTrialBalance
{
    public long Id { get; set; }
    public int ImportBatchId { get; set; }
    public int ClientCompanyId { get; set; }

    // Account identifier from GLBAL.ACCNUM
    public string AccountCode { get; set; } = string.Empty;
    public string? AccountName { get; set; }

    // Which period set: LY | CUR | NY
    public string PeriodSet { get; set; } = string.Empty;
    public int FiscalYear { get; set; }

    // Aggregated from monthly columns (DEBIT1..12 + suffix)
    public decimal BeginBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    // DEBITCLS - CREDITCLS (CUR period only)
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
    public decimal EndBalance { get; set; }

    public bool IsValid { get; set; } = true;
    public string? ValidationError { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
}
