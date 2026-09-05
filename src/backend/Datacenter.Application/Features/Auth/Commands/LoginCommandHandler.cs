using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datacenter.Application.Features.Auth.Commands;

/// <summary>
/// เข้าสู่ระบบ + กันเดารหัสผ่าน (brute force): ใส่ผิดครบเกณฑ์ → ล็อกบัญชีชั่วคราว.
/// ข้อความ error ไม่บอกว่าชื่อผู้ใช้มีจริงหรือไม่ (กัน enumeration) และบันทึก audit ทั้งสำเร็จ/ไม่สำเร็จ.
/// </summary>
public class LoginCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher,
    IOptions<AuthSettings> authOptions)
    : IRequestHandler<LoginCommand, LoginResult>
{
    private const string GenericError = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var settings = authOptions.Value;
        var now = DateTime.UtcNow;
        var username = (request.Username ?? "").Trim();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null)
        {
            // ไม่มีผู้ใช้นี้ — ไม่เขียนฐานข้อมูล แต่คืนข้อความเดียวกับรหัสผิด
            throw new UnauthorizedException(GenericError);
        }

        if (user.LockedUntil is not null && user.LockedUntil > now)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling((user.LockedUntil.Value - now).TotalMinutes));
            throw new UnauthorizedException(
                $"บัญชีถูกล็อกชั่วคราวเนื่องจากใส่รหัสผ่านผิดหลายครั้ง — กรุณาลองใหม่ในอีก {minutes} นาที");
        }

        if (!user.IsActive)
            throw new UnauthorizedException("บัญชีนี้ถูกปิดใช้งาน — กรุณาติดต่อผู้ดูแลระบบ");

        if (!passwordHasher.Verify(request.Password ?? "", user.PasswordHash))
        {
            user.FailedLoginCount++;
            string? lockedMessage = null;
            if (user.FailedLoginCount >= Math.Max(1, settings.MaxFailedAttempts))
            {
                user.LockedUntil = now.AddMinutes(Math.Max(1, settings.LockoutMinutes));
                user.FailedLoginCount = 0;
                lockedMessage =
                    $"ใส่รหัสผ่านผิดครบ {settings.MaxFailedAttempts} ครั้ง — บัญชีถูกล็อก {settings.LockoutMinutes} นาที";
            }

            Audit(user, "LoginFailed", lockedMessage ?? $"ใส่รหัสผ่านผิด (ครั้งที่ {user.FailedLoginCount})", now);
            await db.SaveChangesAsync(CancellationToken.None);
            throw new UnauthorizedException(lockedMessage ?? GenericError);
        }

        // สำเร็จ — เคลียร์ตัวนับ, ออก access + refresh token, ล้าง refresh token ที่หมดอายุของผู้ใช้นี้
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = now;

        var expired = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && (t.ExpiresAt < now || t.RevokedAt != null))
            .ToListAsync(ct);
        if (expired.Count > 0) db.RefreshTokens.RemoveRange(expired);

        var (refreshToken, entity) = AuthTokens.Issue(user, settings, now);
        db.RefreshTokens.Add(entity);

        Audit(user, "Login", "เข้าสู่ระบบสำเร็จ", now);
        await db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = jwtTokenService.GenerateToken(user);
        return new LoginResult(
            UserId:      user.Id,
            Username:    user.Username,
            DisplayName: user.DisplayName,
            Role:        user.Role.ToString(),
            Token:       accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:   expiresAt,
            MustChangePassword: user.MustChangePassword,
            OwnedCompanyIds: await AuthCompanyScope.OwnedCompanyIdsAsync(db, user, ct));
    }

    // เขียน audit ตรง ๆ (IAuditService อ่านผู้ใช้จาก JWT ซึ่งตอน login ยังไม่มี)
    private void Audit(User user, string action, string detail, DateTime now)
        => db.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            UserId = user.Id,
            Username = user.Username,
            Action = action,
            EntityName = "User",
            EntityId = user.Id.ToString(),
            AfterValue = detail,
            CreatedAt = now,
        });
}
