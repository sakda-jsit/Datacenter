using Datacenter.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>POST /api/v1/auth/login</summary>
    // UnauthorizedException ถูกแปลงเป็น 401 โดย ExceptionHandlingMiddleware แบบรวมศูนย์
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>POST /api/v1/auth/refresh</summary>
    [HttpPost("refresh")]
    public IActionResult Refresh() => StatusCode(501, new { message = "Refresh token ยังไม่ได้ implement" });
}
