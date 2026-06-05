using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class CreateFixedAssetCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<CreateFixedAssetCommand, FixedAssetDto>
{
    public async Task<FixedAssetDto> Handle(CreateFixedAssetCommand request, CancellationToken ct)
    {
        var accounts = await FixedAssetAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, request.Data, ct);
        await FixedAssetAccountGuard.ValidateAssetTypeAsync(db, request.Data.AssetTypeId, ct);

        var dup = await db.FixedAssets.AnyAsync(
            x => x.ClientCompanyId == request.ClientCompanyId && x.AssetCode == request.Data.AssetCode, ct);
        if (dup) throw new DomainException($"มีสินทรัพย์รหัส {request.Data.AssetCode} อยู่แล้ว");

        var entity = new FixedAsset { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
        FixedAssetMapper.Apply(entity, request.Data);

        db.FixedAssets.Add(entity);

        await audit.LogAsync("Create", "FixedAsset",
            entityId: $"{request.ClientCompanyId}:{entity.AssetCode}",
            afterValue: $"{entity.AssetName} / ราคาทุน {entity.Cost:N2} / อัตราบัญชี {entity.BookRatePct:N2}%",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        var typeName = await TypeNameAsync(db, entity.AssetTypeId, ct);
        return FixedAssetMapper.ToDto(entity, typeName, accounts);
    }

    internal static async Task<string?> TypeNameAsync(IApplicationDbContext db, int? typeId, CancellationToken ct)
        => typeId is { } id
            ? await db.AssetTypeMasters.Where(t => t.Id == id).Select(t => t.Name).FirstOrDefaultAsync(ct)
            : null;
}
