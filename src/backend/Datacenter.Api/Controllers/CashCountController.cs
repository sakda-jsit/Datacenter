using Datacenter.Application.Features.CashCount.Commands;
using Datacenter.Application.Features.CashCount.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/cash-count")]
public class CashCountController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/cash-count?clientCompanyId=1&amp;fiscalYear=2025 — รายการใบตรวจนับ</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetCashCountsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/cash-count/workpaper?clientCompanyId=1&amp;fiscalYear=2025 — กระดาษทำการ + เทียบ GL</summary>
    [HttpGet("workpaper")]
    public async Task<IActionResult> GetWorkpaper([FromQuery] GetCashCountWorkpaperQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/cash-count/{id}?clientCompanyId=1 — รายละเอียดใบตรวจนับ</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
        => Ok(await mediator.Send(new GetCashCountQuery(id, clientCompanyId), ct));

    /// <summary>POST /api/v1/cash-count — สร้างใบตรวจนับ</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCashCountCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/cash-count/{id} — แก้ไขใบตรวจนับ</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCashCountCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/cash-count/{id}?clientCompanyId=1 — ลบใบตรวจนับ</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteCashCountCommand(id, clientCompanyId), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/cash-count/generate-adjustment — สร้างรายการปรับปรุงเงินสดขาด/เกิน</summary>
    [HttpPost("generate-adjustment")]
    public async Task<IActionResult> GenerateAdjustment([FromBody] GenerateCashCountAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
