using Datacenter.Application.Features.Adjustments.Commands;
using Datacenter.Application.Features.Adjustments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/adjustments")]
public class AdjustmentsController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/adjustments?clientCompanyId=1&amp;fiscalYear=2025 — รายการปรับปรุงทั้งหมดในปีงบ</summary>
    [HttpGet]
    public async Task<IActionResult> GetEntries([FromQuery] GetAdjustmentEntriesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/adjustments/trial-balance?clientCompanyId=1&amp;fiscalYear=2025 — งบทดลองหลังปรับปรุง</summary>
    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetAdjustedTrialBalance(
        [FromQuery] GetAdjustedTrialBalanceQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>POST /api/v1/adjustments — สร้างรายการปรับปรุง</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdjustmentEntryCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/adjustments/{id} — แก้ไขรายการปรับปรุง</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAdjustmentEntryCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/adjustments/{id}?clientCompanyId=1 — ลบรายการปรับปรุง</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteAdjustmentEntryCommand(id, clientCompanyId), ct);
        return NoContent();
    }
}
