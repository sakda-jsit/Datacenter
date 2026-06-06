using Datacenter.Application.Features.Prepaid.Commands;
using Datacenter.Application.Features.Prepaid.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/prepaid")]
public class PrepaidController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/prepaid?clientCompanyId=1 — รายการค่าใช้จ่ายจ่ายล่วงหน้า</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetPrepaidExpensesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/prepaid/workpaper?clientCompanyId=1&amp;fiscalYear=2025 — กระดาษทำการ + เทียบ GL</summary>
    [HttpGet("workpaper")]
    public async Task<IActionResult> GetWorkpaper([FromQuery] GetPrepaidWorkpaperQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/prepaid/{id}?clientCompanyId=1&amp;fiscalYear=2025 — รายละเอียด + ตารางตัดจ่าย</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear, CancellationToken ct)
        => Ok(await mediator.Send(new GetPrepaidExpenseQuery(id, clientCompanyId, fiscalYear), ct));

    /// <summary>POST /api/v1/prepaid — สร้างรายการ</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrepaidExpenseCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/prepaid/{id} — แก้ไขรายการ</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePrepaidExpenseCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/prepaid/{id}?clientCompanyId=1 — ลบรายการ</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeletePrepaidExpenseCommand(id, clientCompanyId), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/prepaid/generate-adjustment — สร้างรายการปรับปรุงตัดจ่ายในปีงบ</summary>
    [HttpPost("generate-adjustment")]
    public async Task<IActionResult> GenerateAdjustment([FromBody] GeneratePrepaidAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
