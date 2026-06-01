using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

public class ClientCompany : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = "00000";
    public string? Address { get; set; }
    public int FiscalYearStartMonth { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<CompanyUserAccess> UserAccesses { get; set; } = [];
}
