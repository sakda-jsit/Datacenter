namespace Datacenter.Application.Common.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Stages an audit log entry. Does NOT save to DB — the caller must call SaveChangesAsync()
    /// to persist both the business entity and the audit log in one atomic write.
    /// </summary>
    Task LogAsync(string action, string entityName, string? entityId = null,
        string? beforeValue = null, string? afterValue = null,
        int? companyId = null, CancellationToken cancellationToken = default);
}
