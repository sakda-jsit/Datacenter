using System.Security.Claims;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Datacenter.Infrastructure.Identity;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return raw is not null && int.TryParse(raw, out var id) ? id : null;
        }
    }
    public string Username => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public int? CurrentCompanyId
    {
        get
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers["X-Company-Id"].FirstOrDefault();
            return int.TryParse(header, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var raw = User?.FindFirstValue(ClaimTypes.Role);
            return raw is not null && Enum.TryParse<UserRole>(raw, out var role) ? role : null;
        }
    }
}
