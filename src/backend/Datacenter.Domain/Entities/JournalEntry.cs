using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime JournalDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
    public ICollection<JournalEntryLine> Lines { get; set; } = [];
}
