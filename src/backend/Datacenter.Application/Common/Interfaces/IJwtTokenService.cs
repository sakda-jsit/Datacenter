using Datacenter.Domain.Entities;

namespace Datacenter.Application.Common.Interfaces;

public interface IJwtTokenService
{
    /// <summary>สร้าง access token (JWT) + เวลาหมดอายุ (UTC) ตามค่าตั้ง Auth:AccessTokenMinutes</summary>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
