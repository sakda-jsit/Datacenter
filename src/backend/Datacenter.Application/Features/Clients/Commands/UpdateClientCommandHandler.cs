using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Clients.Commands;

public class UpdateClientCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpdateClientCommand>
{
    public async Task Handle(UpdateClientCommand request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException("ClientCompany", request.Id);

        var before = client.LegalName;

        client.LegalName = request.LegalName.Trim();   // ชื่อทางการ (Name = ชื่อ Express คง sync จาก import)
        client.TaxId = request.TaxId.Trim();
        client.BranchCode = request.BranchCode.Trim();
        client.Address = request.Address?.Trim();
        client.FiscalYearStartMonth = request.FiscalYearStartMonth;
        client.SsoAccountNo = request.SsoAccountNo?.Trim();
        client.SsoBranchCode = request.SsoBranchCode?.Trim();
        client.Phone = request.Phone?.Trim();
        client.PostalCode = request.PostalCode?.Trim();

        // ที่อยู่แยกช่อง (แก้ได้เอง)
        if (request.AddressDetail is { } a)
        {
            client.AddrBuilding = a.Building?.Trim();
            client.AddrRoomNo = a.RoomNo?.Trim();
            client.AddrFloor = a.Floor?.Trim();
            client.AddrVillage = a.Village?.Trim();
            client.AddrHouseNo = a.HouseNo?.Trim();
            client.AddrMoo = a.Moo?.Trim();
            client.AddrSoi = a.Soi?.Trim();
            client.AddrRoad = a.Road?.Trim();
            client.AddrSubDistrict = a.SubDistrict?.Trim();
            client.AddrDistrict = a.District?.Trim();
            client.AddrProvince = a.Province?.Trim();
        }
        client.BusinessActivity = request.BusinessActivity?.Trim();
        client.IsicCode = request.IsicCode?.Trim();

        client.ModifiedAt = DateTime.UtcNow;
        client.ModifiedBy = currentUser.Username;

        await audit.LogAsync("Update", "ClientCompany", client.Id.ToString(),
            beforeValue: before, afterValue: client.LegalName, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
