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
}
