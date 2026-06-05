using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Queries;

public class GetAssetAccountMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssetAccountMappingsQuery, IReadOnlyList<AssetAccountMappingDto>>
{
    public async Task<IReadOnlyList<AssetAccountMappingDto>> Handle(GetAssetAccountMappingsQuery request, CancellationToken ct)
    {
        var mappings = await db.AssetAccountMappings
            .AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .ToListAsync(ct);

        // จำนวนสินทรัพย์ต่อหมวด + หมวดที่มีในสินทรัพย์แต่ยังไม่มี mapping
        var assetCats = await db.FixedAssets
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.CategoryCode != null && a.CategoryCode != "")
            .GroupBy(a => a.CategoryCode!)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countByCat = assetCats.ToDictionary(x => x.Category, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var accIds = mappings
            .SelectMany(m => new[] { m.AssetAccountId, m.AccumDepreciationAccountId, m.DepreciationExpenseAccountId })
            .Where(id => id is > 0).Select(id => id!.Value).Distinct().ToList();

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => accIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.AccountCode, ct);

        string? Code(int? id) => id is { } v && accounts.TryGetValue(v, out var c) ? c : null;

        var result = mappings
            .Select(m => new AssetAccountMappingDto(
                m.Id, m.ClientCompanyId, m.CategoryCode, m.Description,
                m.AssetAccountId, Code(m.AssetAccountId),
                m.AccumDepreciationAccountId, Code(m.AccumDepreciationAccountId),
                m.DepreciationExpenseAccountId, Code(m.DepreciationExpenseAccountId),
                countByCat.GetValueOrDefault(m.CategoryCode)))
            .ToList();

        // เติมหมวดที่อยู่ในสินทรัพย์แต่ยังไม่มี mapping (Id=0)
        var mapped = mappings.Select(m => m.CategoryCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var c in assetCats.Where(x => !mapped.Contains(x.Category)).OrderBy(x => x.Category))
            result.Add(new AssetAccountMappingDto(0, request.ClientCompanyId, c.Category, null, null, null, null, null, null, null, c.Count));

        return result.OrderBy(r => r.CategoryCode).ToList();
    }
}
