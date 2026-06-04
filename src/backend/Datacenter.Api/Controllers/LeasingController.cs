using Datacenter.Application.Features.Leasing.Commands;
using Datacenter.Application.Features.Leasing.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/leasing")]
public class LeasingController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/leasing?clientCompanyId=1 — รายการสัญญาเช่าซื้อ/เงินกู้</summary>
    [HttpGet]
    public async Task<IActionResult> GetContracts([FromQuery] GetLeaseContractsQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/leasing/workpaper?clientCompanyId=1&amp;fiscalYear=2025 — กระดาษทำการรวม + เทียบ GL</summary>
    [HttpGet("workpaper")]
    public async Task<IActionResult> GetWorkpaper([FromQuery] GetLeaseWorkpaperQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>GET /api/v1/leasing/{id}?clientCompanyId=1&amp;fiscalYear=2025 — รายละเอียด + ตารางตัดบัญชี</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetContract(int id, [FromQuery] int clientCompanyId, [FromQuery] int fiscalYear, CancellationToken ct)
        => Ok(await mediator.Send(new GetLeaseContractQuery(id, clientCompanyId, fiscalYear), ct));

    /// <summary>POST /api/v1/leasing — สร้างสัญญา</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaseContractCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/leasing/{id} — แก้ไขสัญญา</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaseContractCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/leasing/{id}?clientCompanyId=1 — ลบสัญญา</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteLeaseContractCommand(id, clientCompanyId), ct);
        return NoContent();
    }

    /// <summary>POST /api/v1/leasing/generate-adjustment — สร้างรายการปรับปรุงดอกเบี้ยรับรู้ในปี</summary>
    [HttpPost("generate-adjustment")]
    public async Task<IActionResult> GenerateAdjustment([FromBody] GenerateLeaseAdjustmentCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
