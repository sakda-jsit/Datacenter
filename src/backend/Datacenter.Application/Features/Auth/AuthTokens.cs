using System.Security.Cryptography;
using System.Text;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Auth;

/// <summary>
/// ยูทิลิตี refresh token — สร้างค่าสุ่ม (ส่งให้ client) + hash (เก็บลงฐานข้อมูล).
/// ฐานข้อมูลไม่เคยเก็บค่า token จริง จึงสวมสิทธิ์จากข้อมูลที่รั่วไม่ได้.
/// </summary>
internal static class AuthTokens
{
    /// <summary>ค่าสุ่ม 256 บิต (hex) — ส่งกลับให้ client เก็บไว้</summary>
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>ออก refresh token ใบใหม่ให้ผู้ใช้ (คืนค่า token จริง + entity ที่ยังไม่ SaveChanges)</summary>
    public static (string Token, RefreshToken Entity) Issue(User user, AuthSettings settings, DateTime utcNow)
    {
        var token = NewToken();
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            ExpiresAt = utcNow.AddDays(Math.Max(1, settings.RefreshTokenDays)),
            CreatedAt = utcNow,
            CreatedBy = user.Username,
        };
        return (token, entity);
    }
}
