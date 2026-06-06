using Datacenter.Application.Features.Clients.Commands;
using Datacenter.Application.Features.Clients.DTOs;
using Datacenter.Application.Features.Clients.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetClientListQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await mediator.Send(new GetClientDetailQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientRequest body, CancellationToken ct)
    {
        await mediator.Send(new UpdateClientCommand(
            id, body.LegalName, body.TaxId, body.BranchCode, body.Address, body.FiscalYearStartMonth,
            body.SsoAccountNo, body.SsoBranchCode, body.Phone, body.PostalCode), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateClientCommand(id), ct);
        return NoContent();
    }
}
