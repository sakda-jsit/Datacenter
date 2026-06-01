using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;

namespace Datacenter.Infrastructure.Services;

public class AuditService(IApplicationDbContext context, ICurrentUserService currentUser) : IAuditService
{
    public Task LogAsync(string action, string entityName, string? entityId = null,
        string? beforeValue = null, string? afterValue = null,
        int? companyId = null, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            ClientCompanyId = companyId ?? currentUser.CurrentCompanyId,
            UserId = currentUser.UserId,
            Username = currentUser.Username,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            BeforeValue = beforeValue,
            AfterValue = afterValue,
            CreatedAt = DateTime.UtcNow
        };

        // Only stage the log entry — the caller's SaveChangesAsync saves everything atomically.
        context.AuditLogs.Add(log);
        return Task.CompletedTask;
    }
}
