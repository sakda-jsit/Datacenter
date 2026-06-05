using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Queries;

public class GetAssetTypesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssetTypesQuery, IReadOnlyList<AssetTypeDto>>
{
    public async Task<IReadOnlyList<AssetTypeDto>> Handle(GetAssetTypesQuery request, CancellationToken ct)
    {
        var types = await db.AssetTypeMasters
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Code)
            .ToListAsync(ct);

        return types.Select(FixedAssetMapper.ToDto).ToList();
    }
}
