using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/audit-log")]
public class AuditLogController() : ControllerBase
{
    // TODO: implement AuditLogController endpoints
}
