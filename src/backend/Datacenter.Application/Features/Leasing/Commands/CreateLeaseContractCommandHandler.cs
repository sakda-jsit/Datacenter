using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Commands;

public class CreateLeaseContractCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<CreateLeaseContractCommand, LeaseContractDto>
{
    public async Task<LeaseContractDto> Handle(CreateLeaseContractCommand request, CancellationToken ct)
    {
        var accounts = await LeaseAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, request.Data, ct);

        var dup = await db.LeaseContracts.AnyAsync(
            x => x.ClientCompanyId == request.ClientCompanyId && x.ContractNo == request.Data.ContractNo, ct);
        if (dup) throw new DomainException($"มีสัญญาเลขที่ {request.Data.ContractNo} อยู่แล้ว");

        var entity = new LeaseContract { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
        LeasingMapper.Apply(entity, request.Data);

        db.LeaseContracts.Add(entity);

        await audit.LogAsync("Create", "LeaseContract",
            entityId: $"{request.ClientCompanyId}:{entity.ContractNo}",
            afterValue: $"{entity.AssetName} / เงินต้น {entity.FinancedPrincipal:N2} × {entity.NumberOfPeriods} งวด",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return LeasingMapper.ToDto(entity, accounts);
    }
}
