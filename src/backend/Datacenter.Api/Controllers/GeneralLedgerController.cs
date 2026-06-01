using Datacenter.Application.Features.GeneralLedger.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/general-ledger")]
public class GeneralLedgerController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/general-ledger?clientCompanyId=1&amp;year=2024&amp;monthFrom=1&amp;monthTo=3
    /// Optional: &amp;accountId=42 to filter to a single account.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] GetGeneralLedgerQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
