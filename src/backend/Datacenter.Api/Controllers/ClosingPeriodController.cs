using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/closing-period")]
public class ClosingPeriodController() : ControllerBase
{
    // TODO: implement ClosingPeriodController endpoints
}
