using Datacenter.Application.Features.ClosingPeriod.Commands;
using Datacenter.Application.Features.ClosingPeriod.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/closing-period")]
public class ClosingPeriodController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/closing-period?clientCompanyId=1&year=2024 — สถานะการปิดงวดทั้ง 12 เดือน</summary>
    [HttpGet]
    public async Task<IActionResult> GetPeriods(
        [FromQuery] int clientCompanyId,
        [FromQuery] int year,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetClosingPeriodsQuery(clientCompanyId, year), ct));

    /// <summary>GET /api/v1/closing-period/validation?clientCompanyId=1&year=2024&month=6 — ตรวจความพร้อมก่อนปิด</summary>
    [HttpGet("validation")]
    public async Task<IActionResult> GetValidation(
        [FromQuery] int clientCompanyId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetClosingValidationQuery(clientCompanyId, year, month), ct));

    /// <summary>POST /api/v1/closing-period/close — ปิดงวด</summary>
    [HttpPost("close")]
    public async Task<IActionResult> Close([FromBody] CloseClosingPeriodCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>POST /api/v1/closing-period/reopen — เปิดงวดที่ปิดแล้ว (เฉพาะ Admin)</summary>
    [HttpPost("reopen")]
    public async Task<IActionResult> Reopen([FromBody] ReopenClosingPeriodCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>POST /api/v1/closing-period/lock — ล็อกงวดถาวร (เฉพาะ Admin)</summary>
    [HttpPost("lock")]
    public async Task<IActionResult> Lock([FromBody] LockClosingPeriodCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
