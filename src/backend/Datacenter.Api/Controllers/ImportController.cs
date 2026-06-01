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
}
