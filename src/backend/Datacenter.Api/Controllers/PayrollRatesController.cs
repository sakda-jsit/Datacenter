using Datacenter.Application.Features.Payroll.Commands;
using Datacenter.Application.Features.Payroll.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

/// <summary>
/// อัตราเงินสมทบประกันสังคม/กองทุนเงินทดแทน — <b>ค่ากลางของระบบ</b> (ไม่แยกบริษัท),
/// effective-dated (เปลี่ยนรายเดือนในปีเดียวกันได้). อยู่ในเมนูระบบ/ตั้งค่ากลาง.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/payroll-rates")]
public class PayrollRatesController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/payroll-rates — อัตราทั้งหมด (เรียงตามวันที่มีผล)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await mediator.Send(new GetPayrollConfigsQuery(), ct));

    /// <summary>GET /api/v1/payroll-rates/effective?asOf=2025-06-01 — อัตราที่มีผล ณ วันที่</summary>
    [HttpGet("effective")]
    public async Task<IActionResult> GetEffective([FromQuery] DateTime asOf, CancellationToken ct)
        => Ok(await mediator.Send(new GetEffectivePayrollConfigQuery(asOf), ct));

    /// <summary>POST /api/v1/payroll-rates (body: PayrollRateConfigInput)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PayrollRateConfigInput data, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new UpsertPayrollConfigCommand(null, data), ct) });

    /// <summary>PUT /api/v1/payroll-rates/{id} (body: PayrollRateConfigInput)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PayrollRateConfigInput data, CancellationToken ct)
    {
        await mediator.Send(new UpsertPayrollConfigCommand(id, data), ct);
        return NoContent();
    }

    /// <summary>DELETE /api/v1/payroll-rates/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await mediator.Send(new DeletePayrollConfigCommand(id), ct);
        return NoContent();
    }
}
