using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Adjustments.Queries;

public class GetAdjustmentEntriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdjustmentEntriesQuery, IReadOnlyList<AdjustmentEntryDto>>
{
    public async Task<IReadOnlyList<AdjustmentEntryDto>> Handle(GetAdjustmentEntriesQuery request, CancellationToken ct)
    {
        var entries = await db.AdjustmentEntries
            .AsNoTracking()
            .Include(e => e.Lines)
            .Where(e => e.ClientCompanyId == request.ClientCompanyId && e.FiscalYear == request.FiscalYear)
            .OrderBy(e => e.DocumentNo)
            .ToListAsync(ct);

        var accountIds = entries.SelectMany(e => e.Lines).Select(l => l.AccountId).Distinct().ToList();
        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        return entries.Select(e => AdjustmentMapper.ToDto(e, accounts)).ToList();
    }
}
