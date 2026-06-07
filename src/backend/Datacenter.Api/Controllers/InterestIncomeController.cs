using Datacenter.Application.Features.InterestIncome.Commands;
using Datacenter.Application.Features.InterestIncome.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/interest-income")]
public class InterestIncomeController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/interest-income?clientCompanyId=1 — รายการเงินให้กู้</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetInterestLoansQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/interest-income/workpaper?clientCompanyId=1&amp;fiscalYear=2025 — กระดาษทำการ + เทียบ GL</summary>
    [HttpGet("workpaper")]
    public async Task<IActionResult> GetWorkpaper([FromQuery] GetInterestWorkpaperQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/interest-income/{id}?clientCompanyId=1&amp;fiscalYear=2025 — รายละเอียด + ช่วงดอกเบี้ย</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear, CancellationToken ct)
        => Ok(await mediator.Send(new GetInterestLoanQuery(id, clientCompanyId, fiscalYear), ct));

    /// <summary>POST /api/v1/interest-income — สร้างเงินให้กู้</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterestLoanCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/interest-income/{id} — แก้ไขเงินให้กู้</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInterestLoanCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/interest-income/{id}?clientCompanyId=1 — ลบเงินให้กู้</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteInterestLoanCommand(id, clientCompanyId), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/interest-income/generate-adjustment — สร้างรายการปรับปรุงรับรู้ดอกเบี้ยรับ</summary>
    [HttpPost("generate-adjustment")]
    public async Task<IActionResult> GenerateAdjustment([FromBody] GenerateInterestAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
