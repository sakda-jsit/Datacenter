using Datacenter.Application.Features.ReportPackages.Commands;
using Datacenter.Application.Features.ReportPackages.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/report-packages")]
public class ReportPackagesController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/report-packages?clientCompanyId=1&amp;year=0 — รายการชุดรายงานงบ</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetReportPackagesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>POST /api/v1/report-packages — สร้างชุดรายงานใหม่ (version ถัดไป)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportPackageCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>PUT /api/v1/report-packages/{id}/status — เปลี่ยนสถานะ (Draft/Review/Final/Locked)</summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetReportPackageStatusCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("id ใน URL ไม่ตรงกับ body");
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>DELETE /api/v1/report-packages/{id}?clientCompanyId=1 — ลบ (เฉพาะ Draft)</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int clientCompanyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteReportPackageCommand(clientCompanyId, id), ct);
        return NoContent();
    }
}
