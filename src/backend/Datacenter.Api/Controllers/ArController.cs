using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ar")]
public class ArController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => StatusCode(501, new { message = "Accounts Receivable module ยังไม่ได้ implement" });
}
