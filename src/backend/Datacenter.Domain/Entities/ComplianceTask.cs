using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

public class ComplianceTask : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public ComplianceTaskType TaskType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime DueDate { get; set; }
    public ComplianceTaskStatus Status { get; set; } = ComplianceTaskStatus.Pending;
    public int? AssignedUserId { get; set; }
    public string? Note { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedByUserId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
    public User? AssignedUser { get; set; }
    public User? CompletedByUser { get; set; }
}
