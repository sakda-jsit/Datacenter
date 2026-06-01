namespace Datacenter.Domain.Entities;

public class ImportBatchDetail
{
    public long Id { get; set; }
    public int ImportBatchId { get; set; }
    public int RowNumber { get; set; }
    public string? AccountCode { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string RawData { get; set; } = string.Empty;

    public ImportBatch ImportBatch { get; set; } = null!;
}
