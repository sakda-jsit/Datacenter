using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Models;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.AuditLog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditLog.Queries;

public class GetAuditLogsQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = await AuditLogQuerySupport.BuildFilteredAsync(
            db, accessGuard,
            request.ClientCompanyId, request.Action, request.EntityName, request.Search,
            request.FromDate, request.ToDate, ct);

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = await AuditLogQuerySupport.MapAsync(db, logs, ct);
        return PaginatedResult<AuditLogDto>.Create(items, total, request.PageNumber, request.PageSize);
    }
}
