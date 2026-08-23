using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Datacenter.Infrastructure.Identity;

public class JwtTokenService(IConfiguration configuration, IOptions<AuthSettings> authOptions) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var secret = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("ไม่ได้ตั้งค่า Jwt:Key — ตั้งผ่าน environment variable Jwt__Key หรือ appsettings.Local.json");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(Math.Max(5, authOptions.Value.AccessTokenMinutes));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
