using Datacenter.Domain.Enums;

namespace Datacenter.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string Username { get; }
    bool IsAuthenticated { get; }
    int? CurrentCompanyId { get; }
    UserRole? Role { get; }
}
