using Datacenter.Application.Features.Ar.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ar")]
public class ArController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/ar/customers?clientCompanyId=1&amp;includeInactive=false — รายชื่อลูกค้า + ยอดค้าง</summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] GetCustomersQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/ar/invoices?clientCompanyId=1&amp;year=0&amp;outstandingOnly=false&amp;customerCode= — ใบแจ้งหนี้</summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] GetArInvoicesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/ar/aging?clientCompanyId=1&amp;asOf=2026-06-05 — รายงานอายุหนี้</summary>
    [HttpGet("aging")]
    public async Task<IActionResult> GetAging([FromQuery] GetArAgingQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/ar/years?clientCompanyId=1 — ปีที่มีข้อมูลใบแจ้งหนี้</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears([FromQuery] GetArYearsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
