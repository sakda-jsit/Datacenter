using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

public class CompanyUserAccess
{
    public int UserId { get; set; }
    public int ClientCompanyId { get; set; }
    public UserRole RoleInCompany { get; set; }

    public User User { get; set; } = null!;
    public ClientCompany ClientCompany { get; set; } = null!;
}
