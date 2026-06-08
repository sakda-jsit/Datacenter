using Datacenter.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/dashboard/work-tracker?year=2025&amp;month=7 — ภาพรวมงานประจำทุกบริษัทของงวด</summary>
    [HttpGet("work-tracker")]
    public async Task<IActionResult> GetWorkTracker([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
        => Ok(await mediator.Send(new GetWorkTrackerOverviewQuery(year, month), ct));
}
