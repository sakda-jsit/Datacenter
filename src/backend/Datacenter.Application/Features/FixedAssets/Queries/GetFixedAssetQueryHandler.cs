using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Application.Features.FixedAssets.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Queries;

public class GetFixedAssetQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFixedAssetQuery, FixedAssetDetailDto>
{
    public async Task<FixedAssetDetailDto> Handle(GetFixedAssetQuery request, CancellationToken ct)
    {
        var entity = await db.FixedAssets
            .AsNoTracking()
            .Include(x => x.AssetType)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("FixedAsset", request.Id);

        var accountIds = new[]
            {
                entity.AccumDepreciationAccountId, entity.DepreciationExpenseAccountId,
                entity.AssetAccountId ?? 0,
            }
            .Where(id => id > 0).Distinct().ToList();

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        var book = DepreciationEngine.AsOf(entity, entity.BookRatePct, request.FiscalYear);
        var tax = DepreciationEngine.AsOf(entity, entity.TaxRatePct, request.FiscalYear);
        var bookSchedule = DepreciationEngine.BuildSchedule(entity, entity.BookRatePct);
        var taxSchedule = DepreciationEngine.BuildSchedule(entity, entity.TaxRatePct);
        var disposal = DepreciationEngine.Disposal(entity);

        return new FixedAssetDetailDto(
            FixedAssetMapper.ToDto(entity, entity.AssetType?.Name, accounts),
            request.FiscalYear, book, tax, bookSchedule, taxSchedule, disposal);
    }
}
