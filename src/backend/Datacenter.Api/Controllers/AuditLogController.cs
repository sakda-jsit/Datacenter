using Datacenter.Application.Features.AuditLog.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/audit-log")]
public class AuditLogController(IMediator mediator) : ControllerBase
{
    /// <summary>GET /api/v1/audit-log — ดู audit log แบบแบ่งหน้า พร้อมตัวกรอง</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? clientCompanyId,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new GetAuditLogsQuery(clientCompanyId, action, entityName, search, fromDate, toDate, pageNumber, pageSize), ct));

    /// <summary>GET /api/v1/audit-log/export — audit log ทั้งชุดตามตัวกรอง (ไม่แบ่งหน้า) สำหรับ export</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] int? clientCompanyId,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new GetAuditLogExportQuery(clientCompanyId, action, entityName, search, fromDate, toDate), ct));

    /// <summary>GET /api/v1/audit-log/filter-options — ตัวเลือกตัวกรอง (Action/EntityName) ในขอบเขตที่เข้าถึง</summary>
    [HttpGet("filter-options")]
    public async Task<IActionResult> FilterOptions([FromQuery] int? clientCompanyId, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAuditLogFilterOptionsQuery(clientCompanyId), ct));
}
