using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Dashboard.Queries;

public class GetDashboardSummaryQueryHandler(
    IApplicationDbContext db,
    ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // กรองบริษัทตามสิทธิ์: null = Admin เห็นทุกบริษัท
        var companiesQuery = db.ClientCompanies.AsNoTracking();
        var accessibleIds = await accessGuard.GetAccessibleCompanyIdsAsync(ct);
        if (accessibleIds is not null)
            companiesQuery = companiesQuery.Where(c => accessibleIds.Contains(c.Id));

        var totalClients  = await companiesQuery.CountAsync(ct);
        var activeClients = await companiesQuery.CountAsync(c => c.IsActive, ct);

        var companyIds = await companiesQuery.Select(c => c.Id).ToListAsync(ct);

        var pendingTasks = await db.ComplianceTasks
            .CountAsync(t => companyIds.Contains(t.ClientCompanyId) &&
                             t.Status == ComplianceTaskStatus.Pending, ct);

        var overdueTasks = await db.ComplianceTasks
            .CountAsync(t => companyIds.Contains(t.ClientCompanyId) &&
                             t.Status == ComplianceTaskStatus.Pending &&
                             t.DueDate < now, ct);

        var importBatches = await db.ImportBatches
            .CountAsync(b => companyIds.Contains(b.ClientCompanyId) &&
                             b.CreatedAt >= startOfMonth, ct);

        var recentClients = await companiesQuery
            .OrderByDescending(c => c.Id)
            .Take(5)
            .Select(c => new ClientStatusDto(c.Id, c.Code, c.LegalName, c.IsActive))
            .ToListAsync(ct);

        return new DashboardSummaryDto(
            TotalClients:            totalClients,
            ActiveClients:           activeClients,
            PendingComplianceTasks:  pendingTasks,
            OverdueComplianceTasks:  overdueTasks,
            ImportBatchesThisMonth:  importBatches,
            RecentClients:           recentClients);
    }
}
