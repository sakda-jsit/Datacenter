using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class UpdateFixedAssetCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<UpdateFixedAssetCommand, FixedAssetDto>
{
    public async Task<FixedAssetDto> Handle(UpdateFixedAssetCommand request, CancellationToken ct)
    {
        var entity = await db.FixedAssets
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("FixedAsset", request.Id);

        var accounts = await FixedAssetAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, request.Data, ct);
        await FixedAssetAccountGuard.ValidateAssetTypeAsync(db, request.Data.AssetTypeId, ct);

        var dup = await db.FixedAssets.AnyAsync(
            x => x.ClientCompanyId == request.ClientCompanyId
              && x.AssetCode == request.Data.AssetCode
              && x.Id != request.Id, ct);
        if (dup) throw new DomainException($"มีสินทรัพย์รหัส {request.Data.AssetCode} อยู่แล้ว");

        var before = $"{entity.AssetName} / ราคาทุน {entity.Cost:N2} / สถานะ {entity.Status}";

        // สินทรัพย์ที่มาจาก Express: ล็อกฟิลด์ที่ Express เป็นเจ้าของ (รหัส/ชื่อ/ราคาทุน/วัน/ยอดยกมา/หมวด)
        // — แก้ได้เฉพาะฟิลด์ที่ app เป็นเจ้าของ (อัตรา override, บัญชี GL, สถานะ/จำหน่าย, หมายเหตุ).
        // ป้อนเอง (ไม่ได้มาจาก Express) → แก้ได้ทุกฟิลด์.
        if (entity.IsFromExpress)
            FixedAssetMapper.ApplyEditable(entity, request.Data);
        else
            FixedAssetMapper.Apply(entity, request.Data);

        entity.ModifiedBy = currentUser.Username;
        entity.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("Update", "FixedAsset",
            entityId: $"{request.ClientCompanyId}:{entity.AssetCode}",
            beforeValue: before,
            afterValue: $"{entity.AssetName} / ราคาทุน {entity.Cost:N2} / สถานะ {entity.Status}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        var typeName = await CreateFixedAssetCommandHandler.TypeNameAsync(db, entity.AssetTypeId, ct);
        return FixedAssetMapper.ToDto(entity, typeName, accounts);
    }
}
