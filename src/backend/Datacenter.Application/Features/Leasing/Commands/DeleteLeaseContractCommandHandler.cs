using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Commands;

public class DeleteLeaseContractCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<DeleteLeaseContractCommand>
{
    public async Task Handle(DeleteLeaseContractCommand request, CancellationToken ct)
    {
        var entity = await db.LeaseContracts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("LeaseContract", request.Id);

        db.LeaseContracts.Remove(entity);

        await audit.LogAsync("Delete", "LeaseContract",
            entityId: $"{request.ClientCompanyId}:{entity.ContractNo}",
            beforeValue: $"{entity.AssetName} / เงินต้น {entity.FinancedPrincipal:N2}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
