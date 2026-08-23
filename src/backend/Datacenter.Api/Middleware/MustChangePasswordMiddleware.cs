using System.Net;
using System.Text.Json;

namespace Datacenter.Api.Middleware;

/// <summary>
/// ผู้ใช้ที่ยังไม่เปลี่ยนรหัสผ่านชั่วคราว (mustChangePassword) ใช้ API อื่นไม่ได้เลย
/// เหลือเฉพาะเปลี่ยนรหัส/ต่ออายุ/ออกจากระบบ — บังคับที่ฝั่ง server เพราะการกันที่หน้าจอเลี่ยงได้
/// (สถานะอ่านจาก claim ในโทเคน ไม่ต้องแตะฐานข้อมูลทุกคำขอ; โทเคนใหม่ทุกใบสร้างจากสถานะล่าสุดของผู้ใช้)
/// </summary>
public class MustChangePasswordMiddleware(RequestDelegate next)
{
    /// <summary>claim ที่ JwtTokenService ใส่ไว้เมื่อผู้ใช้ต้องเปลี่ยนรหัส</summary>
    public const string ClaimName = "must_change_password";

    private static readonly string[] AllowedPaths =
    [
        "/api/v1/auth/change-password",
        "/api/v1/auth/refresh",
        "/api/v1/auth/logout",
        "/api/v1/auth/login",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true
            && user.FindFirst(ClaimName)?.Value == "1"
            && !AllowedPaths.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "ต้องเปลี่ยนรหัสผ่านก่อนใช้งานระบบ",
                status = (int)HttpStatusCode.Forbidden,
                errors = (object?)null,
            }));
            return;
        }

        await next(context);
    }
}
