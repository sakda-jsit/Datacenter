namespace Datacenter.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int? ClientCompanyId { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
