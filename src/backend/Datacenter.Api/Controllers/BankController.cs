using Datacenter.Application.Features.Bank.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/bank")]
public class BankController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/bank/accounts?clientCompanyId=1 — บัญชีธนาคาร + ยอดคงเหลือปัจจุบัน</summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts([FromQuery] GetBankAccountsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/bank/book?clientCompanyId=1&amp;bankAccountCode=11&amp;year=2025 — สมุดเงินฝาก + ยอดสะสม</summary>
    [HttpGet("book")]
    public async Task<IActionResult> GetBook([FromQuery] GetBankBookQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/bank/years?clientCompanyId=1 — ปีที่มีรายการเดินบัญชี</summary>
    [HttpGet("years")]
    public async Task<IActionResult> GetYears([FromQuery] GetBankYearsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
