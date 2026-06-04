using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Commands;

public class UpdateLeaseContractCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<UpdateLeaseContractCommand, LeaseContractDto>
{
    public async Task<LeaseContractDto> Handle(UpdateLeaseContractCommand request, CancellationToken ct)
    {
        var entity = await db.LeaseContracts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("LeaseContract", request.Id);

        var accounts = await LeaseAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, request.Data, ct);

        var dup = await db.LeaseContracts.AnyAsync(
            x => x.ClientCompanyId == request.ClientCompanyId
              && x.ContractNo == request.Data.ContractNo
              && x.Id != request.Id, ct);
        if (dup) throw new DomainException($"มีสัญญาเลขที่ {request.Data.ContractNo} อยู่แล้ว");

        LeasingMapper.Apply(entity, request.Data);
        entity.ModifiedBy = currentUser.Username;
        entity.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("Update", "LeaseContract",
            entityId: $"{request.ClientCompanyId}:{entity.ContractNo}",
            afterValue: $"{entity.AssetName} / เงินต้น {entity.FinancedPrincipal:N2} × {entity.NumberOfPeriods} งวด",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return LeasingMapper.ToDto(entity, accounts);
    }
}
