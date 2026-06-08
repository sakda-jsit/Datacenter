using Datacenter.Application.Features.ComplianceCalendar.Commands;
using Datacenter.Application.Features.ComplianceCalendar.Queries;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/compliance-calendar")]
public class ComplianceCalendarController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/compliance-calendar/tasks?clientCompanyId=1&year=2024&month=6</summary>
    [HttpGet("tasks")]
    public async Task<IActionResult> GetTasks(
        [FromQuery] int clientCompanyId,
        [FromQuery] int year,
        [FromQuery] int? month,
        [FromQuery] ComplianceTaskStatus? status,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetComplianceTasksQuery(clientCompanyId, year, month, status), ct));

    /// <summary>GET /api/v1/compliance-calendar/dashboard?clientCompanyId=1&year=2024</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int clientCompanyId,
        [FromQuery] int year,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetComplianceDashboardQuery(clientCompanyId, year), ct));

    /// <summary>POST /api/v1/compliance-calendar/generate</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateMonthlyTasksCommand command, CancellationToken ct)
    {
        var created = await mediator.Send(command, ct);
        return Ok(new { created });
    }

    /// <summary>PATCH /api/v1/compliance-calendar/tasks/{id}/status</summary>
    [HttpPatch("tasks/{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest body, CancellationToken ct)
    {
        await mediator.Send(new UpdateTaskStatusCommand(id, body.Status, body.Note), ct);
        return NoContent();
    }

    /// <summary>PATCH /api/v1/compliance-calendar/tasks/{id}/assign</summary>
    [HttpPatch("tasks/{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest body, CancellationToken ct)
    {
        await mediator.Send(new AssignTaskCommand(id, body.UserId), ct);
        return NoContent();
    }

    // ── Template งานประจำ 2 ระดับ (global / เฉพาะบริษัท) ──────────────────────────
    /// <summary>GET /api/v1/compliance-calendar/templates?clientCompanyId= (เว้น/0 = global)</summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] int? clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetComplianceTaskTemplatesQuery(clientCompanyId), ct));

    /// <summary>PUT /api/v1/compliance-calendar/templates — ตั้งค่า (clientCompanyId เว้น/0 = global)</summary>
    [HttpPut("templates")]
    public async Task<IActionResult> UpsertTemplate([FromBody] UpsertComplianceTaskTemplateCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/compliance-calendar/templates?clientCompanyId=&amp;taskType= — ลบ override บริษัท</summary>
    [HttpDelete("templates")]
    public async Task<IActionResult> ResetTemplate([FromQuery] int clientCompanyId, [FromQuery] ComplianceTaskType taskType, CancellationToken ct)
    {
        await mediator.Send(new ResetComplianceTaskTemplateCommand(clientCompanyId, taskType), ct);
        return NoContent();
    }

    public record UpdateStatusRequest(ComplianceTaskStatus Status, string? Note);
    public record AssignRequest(int? UserId);
}
