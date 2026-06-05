using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Queries;

public class GetFixedAssetsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFixedAssetsQuery, FixedAssetListDto>
{
    public async Task<FixedAssetListDto> Handle(GetFixedAssetsQuery request, CancellationToken ct)
    {
        var assets = await db.FixedAssets
            .AsNoTracking()
            .Include(x => x.AssetType)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || x.IsActive))
            .OrderBy(x => x.AssetCode)
            .ToListAsync(ct);

        var items = assets.Select(a => FixedAssetMapper.ToListItem(a, a.AssetType?.Name)).ToList();

        // ความสดของข้อมูล: เวลานำเข้าทะเบียนสินทรัพย์ล่าสุด (FAMAS) — ทั้งบริษัท
        DateTime? dataAsOf = await db.FixedAssets
            .AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId)
            .MaxAsync(x => (DateTime?)x.CreatedAt, ct);

        return new FixedAssetListDto(items, dataAsOf);
    }
}
