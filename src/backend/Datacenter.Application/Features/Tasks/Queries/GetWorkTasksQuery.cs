using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Queries;

/// <summary>งานทั่วไปของบริษัทเดียว (กรองสถานะ/ผู้รับผิดชอบได้)</summary>
public record GetWorkTasksQuery(int ClientCompanyId, WorkTaskStatus? Status, int? AssignedUserId)
    : IRequest<IReadOnlyList<WorkTaskDto>>, IRequireCompanyAccess;

public class GetWorkTasksQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkTasksQuery, IReadOnlyList<WorkTaskDto>>
{
    public async Task<IReadOnlyList<WorkTaskDto>> Handle(GetWorkTasksQuery request, CancellationToken ct)
    {
        var q = db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .Where(t => t.ClientCompanyId == request.ClientCompanyId);

        if (request.Status is { } s) q = q.Where(t => t.Status == s);
        if (request.AssignedUserId is { } uid) q = q.Where(t => t.AssignedUserId == uid);

        var today = DateTime.UtcNow.Date;
        var tasks = await q
            .OrderBy(t => t.Status == WorkTaskStatus.Done || t.Status == WorkTaskStatus.Cancelled)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ToListAsync(ct);

        return tasks.Select(t => WorkTaskMapper.ToDto(t, today)).ToList();
    }
}
