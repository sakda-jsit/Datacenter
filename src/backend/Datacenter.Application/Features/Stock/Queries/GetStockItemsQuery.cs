using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Stock.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Stock.Queries;

/// <summary>รายการสินค้าคงคลังทั้งหมด (ยอดคงเหลือ/มูลค่า)</summary>
public record GetStockItemsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<StockItemDto>>, IRequireCompanyAccess;

public class GetStockItemsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStockItemsQuery, IReadOnlyList<StockItemDto>>
{
    public async Task<IReadOnlyList<StockItemDto>> Handle(GetStockItemsQuery request, CancellationToken ct)
        => await db.StockItems
            .AsNoTracking()
            .Where(s => s.ClientCompanyId == request.ClientCompanyId)
            .OrderBy(s => s.StockCode)
            .Select(s => new StockItemDto(
                s.Id, s.StockCode, s.Name, s.ItemType, s.GroupCode, s.Unit, s.OnHandQty, s.UnitCost, s.StockValue))
            .ToListAsync(ct);
}
