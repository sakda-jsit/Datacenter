using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Application.Features.FixedAssets.Queries;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class UpsertAssetAccountMappingsCommandHandler(
    IApplicationDbContext db,
    IMediator mediator,
    ICurrentUserService currentUser,
    IAuditService audit)
    : IRequestHandler<UpsertAssetAccountMappingsCommand, IReadOnlyList<AssetAccountMappingDto>>
{
    public async Task<IReadOnlyList<AssetAccountMappingDto>> Handle(UpsertAssetAccountMappingsCommand request, CancellationToken ct)
    {
        await ValidateAccountsAsync(request, ct);

        var existing = await db.AssetAccountMappings
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .ToListAsync(ct);
        var byCat = existing.ToDictionary(m => m.CategoryCode, StringComparer.OrdinalIgnoreCase);

        // โหลดสินทรัพย์ของบริษัทไว้เติมบัญชีให้ตัวที่ยังว่าง
        var assets = await db.FixedAssets
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.CategoryCode != null)
            .ToListAsync(ct);

        foreach (var input in request.Mappings)
        {
            var code = input.CategoryCode.Trim();
            if (code.Length == 0) continue;

            if (!byCat.TryGetValue(code, out var m))
            {
                m = new AssetAccountMapping
                {
                    ClientCompanyId = request.ClientCompanyId,
                    CategoryCode = code,
                    CreatedBy = currentUser.Username,
                };
                db.AssetAccountMappings.Add(m);
                byCat[code] = m;
            }
            else
            {
                m.ModifiedBy = currentUser.Username;
                m.ModifiedAt = DateTime.UtcNow;
            }

            m.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            m.AssetAccountId = input.AssetAccountId;
            m.AccumDepreciationAccountId = input.AccumDepreciationAccountId;
            m.DepreciationExpenseAccountId = input.DepreciationExpenseAccountId;

            // เติมบัญชีให้สินทรัพย์หมวดนี้ที่ยังว่าง (ไม่ทับค่าที่ผู้ใช้ตั้งเองไว้แล้ว)
            foreach (var a in assets.Where(a => string.Equals(a.CategoryCode, code, StringComparison.OrdinalIgnoreCase)))
            {
                if (a.AccumDepreciationAccountId == 0 && input.AccumDepreciationAccountId is { } accum)
                    a.AccumDepreciationAccountId = accum;
                if (a.DepreciationExpenseAccountId == 0 && input.DepreciationExpenseAccountId is { } exp)
                    a.DepreciationExpenseAccountId = exp;
                if (a.AssetAccountId is null or 0 && input.AssetAccountId is { } assetAcc)
                    a.AssetAccountId = assetAcc;
            }
        }

        await audit.LogAsync("Update", "AssetAccountMapping",
            entityId: $"{request.ClientCompanyId}",
            afterValue: $"แมพ {request.Mappings.Count} หมวด",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetAssetAccountMappingsQuery(request.ClientCompanyId), ct);
    }

    /// <summary>บัญชีที่ระบุ (ถ้ามี) ต้องอยู่บริษัทเดียวกัน, active และ postable</summary>
    private async Task ValidateAccountsAsync(UpsertAssetAccountMappingsCommand request, CancellationToken ct)
    {
        var ids = request.Mappings
            .SelectMany(m => new[] { m.AssetAccountId, m.AccumDepreciationAccountId, m.DepreciationExpenseAccountId })
            .Where(id => id is > 0).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0) return;

        var accounts = await db.Accounts.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);
        foreach (var id in ids)
        {
            if (!accounts.TryGetValue(id, out var acc))
                throw new DomainException($"ไม่พบบัญชี Id={id}");
            if (acc.ClientCompanyId != request.ClientCompanyId)
                throw new DomainException($"บัญชี {acc.AccountCode} ไม่ได้อยู่ในบริษัทนี้");
            if (!acc.IsActive)
                throw new DomainException($"บัญชี {acc.AccountCode} ถูกปิดใช้งาน");
            if (!acc.IsPostable)
                throw new DomainException($"บัญชี {acc.AccountCode} เป็นบัญชีหัวข้อ (ลงรายการไม่ได้)");
        }
    }
}
