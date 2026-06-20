using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Tasks;

public static class WorkTaskMapper
{
    public static bool IsOverdue(WorkTask t, DateTime today) =>
        t.DueDate.HasValue && t.DueDate.Value.Date < today
        && t.Status is not (WorkTaskStatus.Done or WorkTaskStatus.Cancelled);

    /// <summary>map → DTO (ต้อง Include ClientCompany/AssignedUser/CompletedByUser/Items ก่อน)</summary>
    public static WorkTaskDto ToDto(WorkTask t, DateTime today)
    {
        var clientName = string.IsNullOrWhiteSpace(t.ClientCompany?.LegalName)
            ? (t.ClientCompany?.Name ?? "")
            : t.ClientCompany!.LegalName;
        var items = (t.Items ?? [])
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new WorkTaskItemDto(i.Id, i.Text, i.IsDone, i.SortOrder))
            .ToList();
        return new WorkTaskDto(
            t.Id, t.ClientCompanyId, clientName,
            t.Title, t.Description, t.Category,
            (int)t.Status, WorkTaskNames.StatusName(t.Status),
            (int)t.Priority, WorkTaskNames.PriorityName(t.Priority),
            t.DueDate, t.AssignedUserId, t.AssignedUser?.DisplayName,
            t.CompletedAt, t.CompletedByUser?.DisplayName,
            IsOverdue(t, today),
            (int)t.RecurrenceType, WorkTaskNames.RecurrenceName(t.RecurrenceType), t.RecurrenceInterval,
            items, items.Count(i => i.IsDone), items.Count,
            t.CreatedAt, t.CreatedBy);
    }
}
