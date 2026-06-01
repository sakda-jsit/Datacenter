using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/vat")]
public class VatController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => StatusCode(501, new { message = "VAT module ยังไม่ได้ implement" });
}
