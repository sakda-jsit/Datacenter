using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datacenter.Application.Features.Auth.Commands;

/// <summary>
/// ผู้ใช้เปลี่ยนรหัสผ่านของตัวเอง — ใช้ทั้งกรณีเปลี่ยนตามปกติ และกรณีถูกบังคับเปลี่ยน
/// (mustChangePassword จากผู้ใช้ใหม่/ผู้ดูแลรีเซ็ตรหัสให้). เปลี่ยนแล้ว revoke refresh token ทุกใบ
/// แล้วออกใบใหม่ให้เครื่องที่กำลังใช้งาน (เครื่องอื่นต้อง login ใหม่).
/// </summary>
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<LoginResult>;

public class ChangePasswordCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IOptions<AuthSettings> authOptions)
    : IRequestHandler<ChangePasswordCommand, LoginResult>
{
    public async Task<LoginResult> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("ไม่พบผู้ใช้ปัจจุบัน — กรุณาเข้าสู่ระบบใหม่");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException("ไม่พบผู้ใช้ปัจจุบัน — กรุณาเข้าสู่ระบบใหม่");

        if (!passwordHasher.Verify(request.CurrentPassword ?? "", user.PasswordHash))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["currentPassword"] = ["รหัสผ่านปัจจุบันไม่ถูกต้อง"],
            });

        if ((request.NewPassword ?? "") == (request.CurrentPassword ?? ""))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["newPassword"] = ["รหัสผ่านใหม่ต้องไม่ซ้ำกับรหัสผ่านเดิม"],
            });

        PasswordPolicy.EnsureValid(request.NewPassword, user.Username, "newPassword");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword!);
        user.MustChangePassword = false;
        user.ModifiedAt = now;
        user.ModifiedBy = user.Username;

        var active = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) { t.RevokedAt = now; t.RevokedReason = "password-changed"; }

        var (refreshToken, entity) = AuthTokens.Issue(user, authOptions.Value, now);
        db.RefreshTokens.Add(entity);

        db.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            UserId = user.Id,
            Username = user.Username,
            Action = "ChangePassword",
            EntityName = "User",
            EntityId = user.Id.ToString(),
            AfterValue = "เปลี่ยนรหัสผ่านด้วยตนเอง",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = jwtTokenService.GenerateToken(user);
        return new LoginResult(
            UserId:       user.Id,
            Username:     user.Username,
            DisplayName:  user.DisplayName,
            Role:         user.Role.ToString(),
            Token:        accessToken,
            RefreshToken: refreshToken,
            ExpiresAt:    expiresAt,
            MustChangePassword: false);
    }
}
