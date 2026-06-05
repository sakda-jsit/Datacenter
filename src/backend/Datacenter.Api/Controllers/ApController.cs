using Datacenter.Application.Features.Ap.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ap")]
public class ApController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/ap/suppliers?clientCompanyId=1&amp;includeInactive=false — รายชื่อผู้ขาย + ยอดค้าง</summary>
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] GetSuppliersQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/ap/invoices?clientCompanyId=1&amp;year=0&amp;outstandingOnly=false&amp;supplierCode= — ใบตั้งหนี้</summary>
    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices([FromQuery] GetApInvoicesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/ap/aging?clientCompanyId=1&amp;asOf=2026-06-05 — รายงานอายุหนี้เจ้าหนี้</summary>
    [HttpGet("aging")]
    public async Task<IActionResult> GetAging([FromQuery] GetApAgingQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/ap/years?clientCompanyId=1 — ปีที่มีข้อมูลใบตั้งหนี้</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears([FromQuery] GetApYearsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
