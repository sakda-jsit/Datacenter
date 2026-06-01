namespace Datacenter.Domain.Entities;

public class AccountStatementMapping
{
    public int ClientCompanyId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string RefCode { get; set; } = string.Empty;

    public ClientCompany ClientCompany { get; set; } = null!;
    public StatementLine StatementLine { get; set; } = null!;
}
