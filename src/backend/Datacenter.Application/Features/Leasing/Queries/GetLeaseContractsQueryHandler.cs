using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Leasing.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Queries;

public class GetLeaseContractsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLeaseContractsQuery, IReadOnlyList<LeaseContractListItemDto>>
{
    public async Task<IReadOnlyList<LeaseContractListItemDto>> Handle(GetLeaseContractsQuery request, CancellationToken ct)
    {
        var contracts = await db.LeaseContracts
            .AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || x.IsActive))
            .OrderBy(x => x.ContractNo)
            .ToListAsync(ct);

        return contracts.Select(LeasingMapper.ToListItem).ToList();
    }
}
