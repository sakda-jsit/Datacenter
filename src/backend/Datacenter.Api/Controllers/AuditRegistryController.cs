using Datacenter.Application.Features.AuditRegistry;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

/// <summary>ทะเบียนผู้สอบบัญชี — master ค่ากลางของสำนักงาน (ใช้ซ้ำหลายบริษัท). เมนูตั้งค่ากลาง.</summary>
[Authorize]
[ApiController]
[Route("api/v1/auditors")]
public class AuditorsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await mediator.Send(new GetAuditorsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuditorInput data, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new UpsertAuditorCommand(null, data), ct) });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AuditorInput data, CancellationToken ct)
    {
        await mediator.Send(new UpsertAuditorCommand(id, data), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteAuditorCommand(id), ct);
        return NoContent();
    }
}

/// <summary>ทะเบียนผู้ทำบัญชี — master ค่ากลางของสำนักงาน. เมนูตั้งค่ากลาง.</summary>
[Authorize]
[ApiController]
[Route("api/v1/bookkeepers")]
public class BookkeepersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await mediator.Send(new GetBookkeepersQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookkeeperInput data, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new UpsertBookkeeperCommand(null, data), ct) });

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookkeeperInput data, CancellationToken ct)
    {
        await mediator.Send(new UpsertBookkeeperCommand(id, data), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteBookkeeperCommand(id), ct);
        return NoContent();
    }
}
