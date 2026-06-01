using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/bank-reconciliation")]
public class BankReconciliationController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => StatusCode(501, new { message = "Bank Reconciliation module ยังไม่ได้ implement" });
}
