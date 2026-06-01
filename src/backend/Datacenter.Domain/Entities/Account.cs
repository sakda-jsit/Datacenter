using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

public class Account : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? AccountName2 { get; set; }
    public AccountType AccountType { get; set; }
    public int Level { get; set; }
    public string? ParentCode { get; set; }
    public bool IsPostable { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public ClientCompany ClientCompany { get; set; } = null!;
}
