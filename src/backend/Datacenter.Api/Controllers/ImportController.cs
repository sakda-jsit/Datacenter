using Datacenter.Application.Features.Import.Commands;
using Datacenter.Application.Features.Import.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ImportController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/import — import history (paginated, filterable)</summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] GetImportHistoryQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/import/{id}/validation — validation result for one batch</summary>
    [HttpGet("{id:int}/validation")]
    public async Task<IActionResult> GetValidation(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetImportValidationResultQuery(id), ct));

    /// <summary>POST /api/v1/import/express — start Express DBF import</summary>
    [HttpPost("express")]
    public async Task<IActionResult> StartExpressImport([FromBody] StartExpressImportCommand command, CancellationToken ct)
    {
        var batchId = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetValidation), new { id = batchId }, new { id = batchId });
    }

    /// <summary>POST /api/v1/import/{id}/post — ยกข้อมูล staging เข้าระบบบัญชีจริง (Account + JournalEntry)</summary>
    [HttpPost("{id:int}/post")]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
        => Ok(await mediator.Send(new PostImportBatchCommand(id), ct));

    /// <summary>DELETE /api/v1/import/{id} — ลบ batch และข้อมูล staging/posted ที่เกี่ยวข้อง</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteImportBatchCommand(id), ct);
        return NoContent();
    }
}
