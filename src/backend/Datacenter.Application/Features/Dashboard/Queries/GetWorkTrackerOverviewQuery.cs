using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ComplianceCalendar;
using Datacenter.Application.Features.Dashboard.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Dashboard.Queries;

/// <summary>
/// ภาพรวมงานประจำ (ComplianceTask) ของทุกบริษัทที่ผู้ใช้เข้าถึงได้ ในงวด (ปี/เดือน) — โหมด "ไม่เลือกบริษัท".
/// ไม่ใช่ IRequireCompanyAccess เพราะ aggregate หลายบริษัท (กรองด้วย ICompanyAccessGuard).
/// </summary>
public record GetWorkTrackerOverviewQuery(int Year, int Month) : IRequest<WorkTrackerOverviewDto>;

public class GetWorkTrackerOverviewQueryHandler(IApplicationDbContext db, ICompanyAccessGuard guard)
    : IRequestHandler<GetWorkTrackerOverviewQuery, WorkTrackerOverviewDto>
{
    public async Task<WorkTrackerOverviewDto> Handle(GetWorkTrackerOverviewQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow.Date;
        var accessible = await guard.GetAccessibleCompanyIdsAsync(ct); // null = admin (ทุกบริษัท)

        var taskQuery = db.ComplianceTasks
            .Include(t => t.ClientCompany)
            .Where(t => t.Year == request.Year && t.Month == request.Month);
        if (accessible is not null)
            taskQuery = taskQuery.Where(t => accessible.Contains(t.ClientCompanyId));

        var tasks = await taskQuery.ToListAsync(ct);

        bool IsOverdue(Domain.Entities.ComplianceTask t) =>
            t.Status == ComplianceTaskStatus.Overdue
            || (t.Status != ComplianceTaskStatus.Completed && t.DueDate.Date < now);
        bool IsDueSoon(Domain.Entities.ComplianceTask t) =>
            t.Status != ComplianceTaskStatus.Completed
            && t.DueDate.Date >= now && t.DueDate.Date <= now.AddDays(7);

        // ── KPI ──
        int total = tasks.Count;
        int completed = tasks.Count(t => t.Status == ComplianceTaskStatus.Completed);
        int inProgress = tasks.Count(t => t.Status == ComplianceTaskStatus.InProgress);
        int overdue = tasks.Count(IsOverdue);
        int dueSoon = tasks.Count(IsDueSoon);
        int pending = tasks.Count(t => t.Status == ComplianceTaskStatus.Pending && t.DueDate.Date >= now);

        // ── จำนวนบริษัท ──
        var companyQuery = db.ClientCompanies.Where(c => c.IsActive);
        if (accessible is not null)
            companyQuery = companyQuery.Where(c => accessible.Contains(c.Id));
        int totalActive = await companyQuery.CountAsync(ct);

        var companiesWithTasks = tasks.Select(t => t.ClientCompanyId).Distinct().Count();
        var companiesWithOpenWork = tasks
            .Where(t => t.Status != ComplianceTaskStatus.Completed)
            .Select(t => t.ClientCompanyId).Distinct().Count();

        // ── ต้องจัดการด่วน (overdue + ใกล้ครบกำหนด) ──
        var attention = tasks
            .Where(t => IsOverdue(t) || IsDueSoon(t))
            .OrderByDescending(IsOverdue)            // overdue ก่อน
            .ThenBy(t => t.DueDate)
            .Take(60)
            .Select(t => new WorkTrackerAttentionDto(
                t.Id, t.ClientCompanyId, Name(t),
                (int)t.TaskType, ComplianceTaskHelpers.TaskTypeName(t.TaskType), t.DueDate,
                (int)t.Status, ComplianceTaskHelpers.StatusName(t.Status), IsOverdue(t),
                (int)(t.DueDate.Date - now).TotalDays))
            .ToList();

        // ── ตารางตามบริษัท ──
        var rows = tasks
            .GroupBy(t => t.ClientCompanyId)
            .Select(g => new WorkTrackerCompanyRowDto(
                g.Key, Name(g.First()),
                g.Count(),
                g.Count(t => t.Status == ComplianceTaskStatus.Completed),
                g.Count(t => t.Status != ComplianceTaskStatus.Completed),
                g.Count(IsOverdue),
                g.OrderBy(t => (int)t.TaskType)
                 .Select(t => new WorkTrackerCellDto(
                     (int)t.TaskType, ComplianceTaskHelpers.TaskTypeName(t.TaskType),
                     (int)t.Status, ComplianceTaskHelpers.StatusName(t.Status), IsOverdue(t), t.Id))
                 .ToList()))
            // บริษัทที่เกินกำหนดก่อน → มีงานค้างก่อน → ชื่อ
            .OrderByDescending(r => r.Overdue)
            .ThenByDescending(r => r.Open)
            .ThenBy(r => r.ClientName, StringComparer.Ordinal)
            .ToList();

        return new WorkTrackerOverviewDto(
            request.Year, request.Month,
            total, completed, inProgress, pending, overdue, dueSoon,
            companiesWithOpenWork, companiesWithTasks, totalActive, Math.Max(totalActive - companiesWithTasks, 0),
            attention, rows);
    }

    private static string Name(Domain.Entities.ComplianceTask t)
        => string.IsNullOrWhiteSpace(t.ClientCompany?.LegalName)
            ? (t.ClientCompany?.Name ?? "")
            : t.ClientCompany!.LegalName;
}
