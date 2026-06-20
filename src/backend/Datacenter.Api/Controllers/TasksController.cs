using Datacenter.Application.Features.Tasks.Commands;
using Datacenter.Application.Features.Tasks.Queries;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/work-tasks")]
public class TasksController(IMediator mediator) : ControllerBase
{
    /// <summary>งานทั่วไปของบริษัทเดียว</summary>
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] GetWorkTasksQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>ผู้ใช้ที่มอบหมายงานในบริษัทนี้ได้</summary>
    [HttpGet("assignable-users")]
    public async Task<IActionResult> GetAssignableUsers([FromQuery] GetAssignableUsersQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>Workboard ข้ามบริษัท (รวม WorkTask + ComplianceTask)</summary>
    [HttpGet("board")]
    public async Task<IActionResult> GetBoard(
        [FromQuery] int? assignedUserId, [FromQuery] bool openOnly = true,
        [FromQuery] DateTime? dueBefore = null, [FromQuery] bool includeCompliance = true,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetWorkboardQuery(assignedUserId, openOnly, dueBefore, includeCompliance), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkTaskCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkTaskCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command with { Id = id }, ct));

    public record StatusRequest(WorkTaskStatus Status);
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] StatusRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateWorkTaskStatusCommand(id, body.Status), ct));

    public record AssignRequest(int? UserId);
    [HttpPatch("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new AssignWorkTaskCommand(id, body.UserId), ct));

    public record ToggleItemRequest(bool IsDone);
    [HttpPatch("{id:int}/items/{itemId:int}")]
    public async Task<IActionResult> ToggleItem(int id, int itemId, [FromBody] ToggleItemRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleWorkTaskItemCommand(id, itemId, body.IsDone), ct));

    /// <summary>ส่งอีเมลเตือนผู้รับผิดชอบ (งานค้าง/ใกล้ครบกำหนด) — Admin เท่านั้น</summary>
    [HttpPost("send-reminders")]
    public async Task<IActionResult> SendReminders([FromQuery] int daysAhead = 3, CancellationToken ct = default)
        => Ok(await mediator.Send(new SendTaskRemindersCommand(daysAhead), ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteWorkTaskCommand(id), ct);
        return NoContent();
    }
}
