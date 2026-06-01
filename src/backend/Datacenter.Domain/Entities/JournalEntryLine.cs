using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

public class JournalEntryLine : BaseEntity
{
    public int JournalEntryId { get; set; }
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }

    public JournalEntry JournalEntry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
