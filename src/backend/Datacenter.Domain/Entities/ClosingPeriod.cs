using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

public class ClosingPeriod : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public PeriodStatus Status { get; set; } = PeriodStatus.Open;
    public int? ClosedByUserId { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
