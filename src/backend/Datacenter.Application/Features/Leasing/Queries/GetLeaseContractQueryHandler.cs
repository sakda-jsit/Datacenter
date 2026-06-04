using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Application.Features.Leasing.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Queries;

public class GetLeaseContractQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLeaseContractQuery, LeaseContractDetailDto>
{
    public async Task<LeaseContractDetailDto> Handle(GetLeaseContractQuery request, CancellationToken ct)
    {
        var entity = await db.LeaseContracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("LeaseContract", request.Id);

        var accountIds = new[]
            {
                entity.LiabilityAccountId, entity.InterestExpenseAccountId,
                entity.DeferredInterestAccountId ?? 0, entity.InputVatUndueAccountId ?? 0,
            }
            .Where(id => id > 0).Distinct().ToList();

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        var schedule = LeaseAmortizationEngine.BuildSchedule(entity);
        var yearEnd = LeaseAmortizationEngine.BuildYearEndSummary(schedule, request.FiscalYear);

        return new LeaseContractDetailDto(
            LeasingMapper.ToDto(entity, accounts), yearEnd, schedule);
    }
}
