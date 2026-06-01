using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.TrialBalance.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.TrialBalance.Queries;

public class GetPeriodStatusQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPeriodStatusQuery, IReadOnlyList<PeriodStatusDto>>
{
    public async Task<IReadOnlyList<PeriodStatusDto>> Handle(GetPeriodStatusQuery request, CancellationToken ct)
    {
        var closed = await db.ClosingPeriods
            .AsNoTracking()
            .Where(p => p.ClientCompanyId == request.ClientCompanyId && p.Year == request.Year)
            .ToDictionaryAsync(p => p.Month, ct);

        // Always return all 12 months; missing months are Open
        return Enumerable.Range(1, 12)
            .Select(m => closed.TryGetValue(m, out var p)
                ? new PeriodStatusDto(request.Year, m, p.Status, p.ClosedAt)
                : new PeriodStatusDto(request.Year, m, PeriodStatus.Open, null))
            .ToList();
    }
}
