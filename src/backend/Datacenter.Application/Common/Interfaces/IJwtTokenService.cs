using Datacenter.Domain.Entities;

namespace Datacenter.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
