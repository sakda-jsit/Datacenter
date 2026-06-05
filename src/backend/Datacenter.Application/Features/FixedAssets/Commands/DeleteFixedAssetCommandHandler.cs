using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class DeleteFixedAssetCommandHandler(
    IApplicationDbContext db,
    IAuditService audit)
    : IRequestHandler<DeleteFixedAssetCommand>
{
    public async Task Handle(DeleteFixedAssetCommand request, CancellationToken ct)
    {
        var entity = await db.FixedAssets
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("FixedAsset", request.Id);

        db.FixedAssets.Remove(entity);

        await audit.LogAsync("Delete", "FixedAsset",
            entityId: $"{entity.ClientCompanyId}:{entity.AssetCode}",
            beforeValue: $"{entity.AssetName} / ราคาทุน {entity.Cost:N2}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
