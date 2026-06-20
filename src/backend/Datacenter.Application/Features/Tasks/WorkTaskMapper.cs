using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Tasks;

public static class WorkTaskMapper
{
    public static bool IsOverdue(WorkTask t, DateTime today) =>
        t.DueDate.HasValue && t.DueDate.Value.Date < today
        && t.Status is not (WorkTaskStatus.Done or WorkTaskStatus.Cancelled);

    /// <summary>map → DTO (ต้อง Include ClientCompany/AssignedUser/CompletedByUser ก่อน)</summary>
    public static WorkTaskDto ToDto(WorkTask t, DateTime today)
    {
        var clientName = string.IsNullOrWhiteSpace(t.ClientCompany?.LegalName)
            ? (t.ClientCompany?.Name ?? "")
            : t.ClientCompany!.LegalName;
        return new WorkTaskDto(
            t.Id, t.ClientCompanyId, clientName,
            t.Title, t.Description, t.Category,
            (int)t.Status, WorkTaskNames.StatusName(t.Status),
            (int)t.Priority, WorkTaskNames.PriorityName(t.Priority),
            t.DueDate, t.AssignedUserId, t.AssignedUser?.DisplayName,
            t.CompletedAt, t.CompletedByUser?.DisplayName,
            IsOverdue(t, today), t.CreatedAt, t.CreatedBy);
    }
}
