using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datacenter.Application.Features.Auth.Commands;

/// <summary>ต่ออายุการเข้าใช้งานด้วย refresh token (rotation: ใบเดิมถูก revoke ทันที)</summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;

public class RefreshTokenCommandHandler(
    IApplicationDbContext db,
    IJwtTokenService jwtTokenService,
    IOptions<AuthSettings> authOptions)
    : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var settings = authOptions.Value;
        var now = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException("ไม่พบ refresh token");

        var hash = AuthTokens.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
            throw new UnauthorizedException("refresh token ไม่ถูกต้อง — กรุณาเข้าสู่ระบบใหม่");

        // ใบที่ถูก revoke แล้วถูกนำมาใช้ซ้ำ = สัญญาณว่า token รั่ว → ตัดทั้งสายของผู้ใช้นี้
        if (stored.RevokedAt is not null)
        {
            var active = await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ToListAsync(ct);
            foreach (var t in active) { t.RevokedAt = now; t.RevokedReason = "reused"; }
            await db.SaveChangesAsync(CancellationToken.None);
            throw new UnauthorizedException("refresh token ถูกใช้ไปแล้ว — กรุณาเข้าสู่ระบบใหม่");
        }

        if (stored.ExpiresAt <= now)
            throw new UnauthorizedException("การเข้าใช้งานหมดอายุ — กรุณาเข้าสู่ระบบใหม่");

        var user = stored.User;
        if (!user.IsActive)
            throw new UnauthorizedException("บัญชีนี้ถูกปิดใช้งาน — กรุณาติดต่อผู้ดูแลระบบ");
        if (user.LockedUntil is not null && user.LockedUntil > now)
            throw new UnauthorizedException("บัญชีถูกล็อกชั่วคราว — กรุณาลองใหม่ภายหลัง");

        stored.RevokedAt = now;
        stored.RevokedReason = "rotated";

        var (refreshToken, entity) = AuthTokens.Issue(user, settings, now);
        db.RefreshTokens.Add(entity);
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
            MustChangePassword: user.MustChangePassword);
    }
}
