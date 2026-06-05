using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

public class GetComplianceDashboardQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetComplianceDashboardQuery, ComplianceDashboardDto>
{
    public async Task<ComplianceDashboardDto> Handle(GetComplianceDashboardQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.FindAsync([request.ClientCompanyId], ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var tasks = await db.ComplianceTasks
            .Include(t => t.AssignedUser)
            .Include(t => t.CompletedByUser)
            .Where(t => t.ClientCompanyId == request.ClientCompanyId && t.Year == request.Year)
            .ToListAsync(ct);

        var now = DateTime.UtcNow.Date;

        // Monthly summary (always 12 months) — IsOverdue computed in memory, no DB write here
        var months = Enumerable.Range(1, 12).Select(m =>
        {
            var monthTasks = tasks.Where(t => t.Month == m).ToList();
            return new MonthSummaryDto(
                m,
                monthTasks.Count,
                monthTasks.Count(t => t.Status == ComplianceTaskStatus.Completed),
                monthTasks.Count(t => t.Status == ComplianceTaskStatus.InProgress),
                monthTasks.Count(t => t.Status == ComplianceTaskStatus.Pending),
                monthTasks.Count(t => t.Status == ComplianceTaskStatus.Overdue
                                   || (t.Status == ComplianceTaskStatus.Pending && t.DueDate.Date < now))
            );
        }).ToList();

        // Effective overdue count includes pending-but-past-due
        int totalOverdue = tasks.Count(t =>
            t.Status == ComplianceTaskStatus.Overdue ||
            (t.Status == ComplianceTaskStatus.Pending && t.DueDate.Date < now));

        var upcomingTasks = tasks
            .Where(t => t.Status != ComplianceTaskStatus.Completed
                     && t.DueDate.Date >= now
                     && t.DueDate.Date <= now.AddDays(7))
            .OrderBy(t => t.DueDate)
            .Select(t => ToDto(t, now))
            .ToList();

        return new ComplianceDashboardDto(
            request.ClientCompanyId,
            client.Code,
            client.LegalName,
            request.Year,
            months,
            totalOverdue,
            upcomingTasks
        );
    }

    private static ComplianceTaskDto ToDto(Domain.Entities.ComplianceTask t, DateTime now) =>
        new(t.Id, t.ClientCompanyId, t.ClientCompany?.Code ?? "", t.ClientCompany?.LegalName ?? "",
            t.TaskType, ComplianceTaskHelpers.TaskTypeName(t.TaskType),
            t.Year, t.Month, t.DueDate,
            t.Status, ComplianceTaskHelpers.StatusName(t.Status),
            t.AssignedUserId, t.AssignedUser?.DisplayName,
            t.Note, t.CompletedAt, t.CompletedByUserId, t.CompletedByUser?.DisplayName,
            t.Status != ComplianceTaskStatus.Completed && t.DueDate.Date < now);
}
