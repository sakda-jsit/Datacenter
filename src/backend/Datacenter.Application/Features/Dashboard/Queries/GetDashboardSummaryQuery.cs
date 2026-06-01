using MediatR;

namespace Datacenter.Application.Features.Dashboard.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public record DashboardSummaryDto(
    int TotalClients,
    int ActiveClients,
    int PendingComplianceTasks,
    int OverdueComplianceTasks,
    int ImportBatchesThisMonth,
    IReadOnlyList<ClientStatusDto> RecentClients);

public record ClientStatusDto(int Id, string Code, string Name, bool IsActive);
