using Datacenter.Application.Features.Stock.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/stock")]
public class StockController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/stock?clientCompanyId=1 — รายการสินค้าคงคลัง</summary>
    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] GetStockItemsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/stock/valuation?clientCompanyId=1&amp;fiscalYear=2025 — มูลค่าสินค้าคงเหลือ + เทียบ GL</summary>
    [HttpGet("valuation")]
    public async Task<IActionResult> GetValuation([FromQuery] GetStockValuationQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
