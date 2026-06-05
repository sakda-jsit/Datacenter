using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Queries;

public class GetFixedAssetsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFixedAssetsQuery, IReadOnlyList<FixedAssetListItemDto>>
{
    public async Task<IReadOnlyList<FixedAssetListItemDto>> Handle(GetFixedAssetsQuery request, CancellationToken ct)
    {
        var assets = await db.FixedAssets
            .AsNoTracking()
            .Include(x => x.AssetType)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || x.IsActive))
            .OrderBy(x => x.AssetCode)
            .ToListAsync(ct);

        return assets.Select(a => FixedAssetMapper.ToListItem(a, a.AssetType?.Name)).ToList();
    }
}
