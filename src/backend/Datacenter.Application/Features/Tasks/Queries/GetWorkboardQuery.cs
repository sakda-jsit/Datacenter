using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ComplianceCalendar;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Queries;

/// <summary>
/// Workboard ข้ามบริษัท — รวมงานทั่วไป (WorkTask) + งานภาษี (ComplianceTask) ของทุกบริษัทที่ผู้ใช้เข้าถึงได้.
/// ไม่ใช่ IRequireCompanyAccess (aggregate หลายบริษัท → กรองด้วย ICompanyAccessGuard เหมือน GetWorkTrackerOverview).
/// </summary>
public record GetWorkboardQuery(
    int? AssignedUserId,    // null = ทุกคน
    bool OpenOnly,          // true = เฉพาะงานที่ยังไม่เสร็จ/ยกเลิก
    DateTime? DueBefore,    // กรองครบกำหนดก่อนวันที่
    bool IncludeCompliance) // รวมงานภาษีจาก ComplianceTask ด้วยหรือไม่
    : IRequest<IReadOnlyList<WorkItemDto>>;

public class GetWorkboardQueryHandler(IApplicationDbContext db, ICompanyAccessGuard guard)
    : IRequestHandler<GetWorkboardQuery, IReadOnlyList<WorkItemDto>>
{
    public async Task<IReadOnlyList<WorkItemDto>> Handle(GetWorkboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var accessible = await guard.GetAccessibleCompanyIdsAsync(ct); // null = admin (ทุกบริษัท)

        static string CompanyName(Domain.Entities.ClientCompany? c)
            => c is null ? "" : (string.IsNullOrWhiteSpace(c.LegalName) ? c.Name : c.LegalName);

        var items = new List<WorkItemDto>();

        // ── งานทั่วไป (WorkTask) ──
        var taskQ = db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser)
            .AsQueryable();
        if (accessible is not null) taskQ = taskQ.Where(t => accessible.Contains(t.ClientCompanyId));
        if (request.AssignedUserId is { } uid) taskQ = taskQ.Where(t => t.AssignedUserId == uid);
        if (request.OpenOnly) taskQ = taskQ.Where(t => t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled);
        if (request.DueBefore is { } db1) taskQ = taskQ.Where(t => t.DueDate != null && t.DueDate < db1);

        foreach (var t in await taskQ.ToListAsync(ct))
            items.Add(new WorkItemDto(
                "Task", t.Id, t.ClientCompanyId, CompanyName(t.ClientCompany),
                t.Title, (int)t.Status, WorkTaskNames.StatusName(t.Status),
                (int)t.Priority, WorkTaskNames.PriorityName(t.Priority),
                t.DueDate, t.AssignedUserId, t.AssignedUser?.DisplayName,
                WorkTaskMapper.IsOverdue(t, today),
                t.DueDate.HasValue ? (int)(t.DueDate.Value.Date - today).TotalDays : null));

        // ── งานภาษี (ComplianceTask) — read-only ──
        if (request.IncludeCompliance)
        {
            var compQ = db.ComplianceTasks.AsNoTracking()
                .Include(t => t.ClientCompany).Include(t => t.AssignedUser)
                .AsQueryable();
            if (accessible is not null) compQ = compQ.Where(t => accessible.Contains(t.ClientCompanyId));
            if (request.AssignedUserId is { } uid2) compQ = compQ.Where(t => t.AssignedUserId == uid2);
            if (request.OpenOnly) compQ = compQ.Where(t => t.Status != ComplianceTaskStatus.Completed);
            if (request.DueBefore is { } db2) compQ = compQ.Where(t => t.DueDate < db2);

            foreach (var t in await compQ.ToListAsync(ct))
            {
                bool overdue = t.Status == ComplianceTaskStatus.Overdue
                            || (t.Status != ComplianceTaskStatus.Completed && t.DueDate.Date < today);
                items.Add(new WorkItemDto(
                    "Compliance", t.Id, t.ClientCompanyId, CompanyName(t.ClientCompany),
                    $"{ComplianceTaskHelpers.TaskTypeName(t.TaskType)} ({t.Month}/{t.Year})",
                    (int)t.Status, ComplianceTaskHelpers.StatusName(t.Status),
                    null, null,
                    t.DueDate, t.AssignedUserId, t.AssignedUser?.DisplayName,
                    overdue, (int)(t.DueDate.Date - today).TotalDays));
            }
        }

        // เกินกำหนดก่อน → ครบกำหนดเร็วก่อน → ความสำคัญสูงก่อน
        return items
            .OrderByDescending(i => i.IsOverdue)
            .ThenBy(i => i.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(i => i.Priority ?? 1)
            .ToList();
    }
}
