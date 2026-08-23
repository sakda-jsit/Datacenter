using Datacenter.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datacenter.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>POST /api/v1/auth/login — เข้าสู่ระบบ (ใส่รหัสผิดหลายครั้งจะถูกล็อกชั่วคราว)</summary>
    // UnauthorizedException ถูกแปลงเป็น 401 โดย ExceptionHandlingMiddleware แบบรวมศูนย์
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>POST /api/v1/auth/refresh (body: { refreshToken }) — ต่ออายุการเข้าใช้งาน (rotation)</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new RefreshTokenCommand(body.RefreshToken), ct));

    /// <summary>POST /api/v1/auth/logout (body: { refreshToken }) — ยกเลิก refresh token ใบที่ถืออยู่</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest body, CancellationToken ct)
    {
        await mediator.Send(new LogoutCommand(body.RefreshToken), ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/auth/change-password (body: { currentPassword, newPassword }) — เปลี่ยนรหัสผ่านของตัวเอง.
    /// ใช้ได้ทั้งกรณีปกติและกรณีถูกบังคับเปลี่ยน (mustChangePassword) — คืน token ชุดใหม่ให้ใช้ต่อทันที.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest body, CancellationToken ct)
        => Ok(await mediator.Send(new ChangePasswordCommand(body.CurrentPassword, body.NewPassword), ct));

    public record RefreshTokenRequest(string RefreshToken);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
