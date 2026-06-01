using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/payroll")]
public class PayrollController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => StatusCode(501, new { message = "Payroll module ยังไม่ได้ implement" });
}
