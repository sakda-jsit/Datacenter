using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Clients.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Clients.Queries;

public class GetClientDetailQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetClientDetailQuery, ClientDetailDto>
{
    public async Task<ClientDetailDto> Handle(GetClientDetailQuery request, CancellationToken ct)
    {
        var c = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(ClientDetailDto), request.Id);

        return new ClientDetailDto(c.Id, c.Code, c.Name, c.LegalName, c.TaxId, c.BranchCode, c.Address, c.FiscalYearStartMonth, c.IsActive,
            c.SsoAccountNo, c.SsoBranchCode, c.Phone, c.PostalCode,
            new ClientAddressDto(c.AddrBuilding, c.AddrRoomNo, c.AddrFloor, c.AddrVillage, c.AddrHouseNo,
                c.AddrMoo, c.AddrSoi, c.AddrRoad, c.AddrSubDistrict, c.AddrDistrict, c.AddrProvince),
            c.BusinessActivity);
    }
}
