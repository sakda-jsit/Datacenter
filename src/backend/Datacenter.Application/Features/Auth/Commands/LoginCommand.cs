using MediatR;

namespace Datacenter.Application.Features.Auth.Commands;

public record LoginCommand(string Username, string Password)
    : IRequest<LoginResult>;

/// <summary>
/// ผลการเข้าสู่ระบบ — access token อายุสั้น (ต่ออายุด้วย refreshToken) +
/// mustChangePassword = true เมื่อเป็นรหัสที่ผู้ดูแลตั้งให้ ต้องเปลี่ยนก่อนใช้งาน.
/// </summary>
public record LoginResult(
    int UserId,
    string Username,
    string DisplayName,
    string Role,
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    bool MustChangePassword);
