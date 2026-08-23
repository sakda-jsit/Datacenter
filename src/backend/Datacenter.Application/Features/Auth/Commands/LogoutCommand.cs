using Datacenter.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Auth.Commands;

/// <summary>ออกจากระบบ — revoke refresh token ใบที่ client ถืออยู่ (ไม่ error ถ้าหาไม่เจอ)</summary>
public record LogoutCommand(string? RefreshToken) : IRequest<Unit>;

public class LogoutCommandHandler(IApplicationDbContext db) : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return Unit.Value;

        var hash = AuthTokens.Hash(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (stored is null) return Unit.Value;

        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedReason = "logout";
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
