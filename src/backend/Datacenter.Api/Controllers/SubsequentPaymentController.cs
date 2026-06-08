using Datacenter.Application.Features.SubsequentPayment.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/subsequent-payment")]
public class SubsequentPaymentController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/subsequent-payment/check?clientCompanyId=1&amp;fiscalYear=2025
    /// — ตรวจการจ่ายชำระหลังปิดงบ (RPT-019): รายการค้างจ่ายปีปิดงบ ถูกจ่ายจริงในปีถัดไปหรือยัง
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> GetCheck([FromQuery] GetSubsequentPaymentCheckQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));
}
