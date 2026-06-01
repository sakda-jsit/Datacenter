using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

public class GetComplianceTasksQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetComplianceTasksQuery, IReadOnlyList<ComplianceTaskDto>>
{
    public async Task<IReadOnlyList<ComplianceTaskDto>> Handle(GetComplianceTasksQuery request, CancellationToken ct)
    {
        var query = db.ComplianceTasks
            .Include(t => t.ClientCompany)
            .Include(t => t.AssignedUser)
            .Include(t => t.CompletedByUser)
            .Where(t => t.ClientCompanyId == request.ClientCompanyId && t.Year == request.Year);

        if (request.Month.HasValue)
            query = query.Where(t => t.Month == request.Month.Value);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        var tasks = await query.OrderBy(t => t.Month).ThenBy(t => t.TaskType).ToListAsync(ct);
        var now = DateTime.UtcNow.Date;

        return tasks.Select(t => new ComplianceTaskDto(
            t.Id,
            t.ClientCompanyId,
            t.ClientCompany.Code,
            t.ClientCompany.Name,
            t.TaskType,
            ComplianceTaskHelpers.TaskTypeName(t.TaskType),
            t.Year,
            t.Month,
            t.DueDate,
            t.Status,
            ComplianceTaskHelpers.StatusName(t.Status),
            t.AssignedUserId,
            t.AssignedUser?.DisplayName,
            t.Note,
            t.CompletedAt,
            t.CompletedByUserId,
            t.CompletedByUser?.DisplayName,
            t.Status != ComplianceTaskStatus.Completed && t.DueDate.Date < now
        )).ToList();
    }
}
