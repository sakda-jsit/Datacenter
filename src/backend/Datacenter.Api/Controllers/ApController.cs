using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ap")]
public class ApController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => StatusCode(501, new { message = "Accounts Payable module ยังไม่ได้ implement" });
}
