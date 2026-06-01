using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/tax-report")]
public class TaxReportController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => StatusCode(501, new { message = "Tax Report module ยังไม่ได้ implement" });
}
