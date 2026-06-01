using Datacenter.Application.Features.TrialBalance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/trial-balance")]
public class TrialBalanceController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/trial-balance?clientCompanyId=1&amp;year=2024&amp;monthFrom=1&amp;monthTo=12
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] GetTrialBalanceQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>
    /// GET /api/v1/trial-balance/accounts?clientCompanyId=1
    /// </summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts([FromQuery] GetAccountListQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>
    /// GET /api/v1/trial-balance/periods?clientCompanyId=1&amp;year=2024
    /// </summary>
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriodStatus([FromQuery] GetPeriodStatusQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
