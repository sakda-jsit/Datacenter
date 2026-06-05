using Datacenter.Application.Features.Vat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/vat")]
public class VatController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/vat/report?clientCompanyId=1&amp;year=2025 — รายงาน ภ.พ.30 รายเดือน</summary>
    [HttpGet("report")]
    public async Task<IActionResult> GetReport([FromQuery] GetVatReportQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/vat/years?clientCompanyId=1 — ปีภาษีที่มีข้อมูล</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears([FromQuery] GetVatYearsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/vat?clientCompanyId=1&amp;year=2025&amp;month=0&amp;vatType= — รายละเอียดภาษีซื้อ/ขาย</summary>
    [HttpGet]
    public async Task<IActionResult> GetEntries([FromQuery] GetVatEntriesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
