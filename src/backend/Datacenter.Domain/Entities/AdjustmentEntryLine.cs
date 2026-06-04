using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>บรรทัดของรายการปรับปรุงปิดงบ — เดบิต/เครดิตต่อบัญชี</summary>
public class AdjustmentEntryLine : BaseEntity
{
    public int AdjustmentEntryId { get; set; }
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Description { get; set; }

    public AdjustmentEntry AdjustmentEntry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
