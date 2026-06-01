using Datacenter.Application.Features.FinancialStatement.Commands;
using Datacenter.Application.Features.FinancialStatement.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/financial-statement")]
public class FinancialStatementController(IMediator mediator) : ControllerBase
{
    // ── Reports ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/financial-statement/balance-sheet?clientCompanyId=1&fiscalYear=2024</summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] GetBalanceSheetQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/financial-statement/profit-loss?clientCompanyId=1&fiscalYear=2024</summary>
    [HttpGet("profit-loss")]
    public async Task<IActionResult> GetProfitLoss([FromQuery] GetProfitLossQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    // ── Account Mappings ──────────────────────────────────────────────────────

    /// <summary>GET /api/v1/financial-statement/mappings?clientCompanyId=1</summary>
    [HttpGet("mappings")]
    public async Task<IActionResult> GetMappings([FromQuery] GetAccountMappingsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>PUT /api/v1/financial-statement/mappings</summary>
    [HttpPut("mappings")]
    public async Task<IActionResult> UpsertMapping([FromBody] UpsertAccountMappingCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/financial-statement/mappings/{clientCompanyId}/{accountCode}</summary>
    [HttpDelete("mappings/{clientCompanyId:int}/{accountCode}")]
    public async Task<IActionResult> DeleteMapping(int clientCompanyId, string accountCode, CancellationToken ct)
    {
        await mediator.Send(new DeleteAccountMappingCommand(clientCompanyId, accountCode), ct);
        return NoContent();
    }

    // ── External Inputs (X4 tax) ──────────────────────────────────────────────

    /// <summary>PUT /api/v1/financial-statement/external-inputs</summary>
    [HttpPut("external-inputs")]
    public async Task<IActionResult> UpsertExternalInput([FromBody] UpsertExternalInputCommand command, CancellationToken ct)
    {
        await mediator.Send(command, ct);
        return NoContent();
    }
}
