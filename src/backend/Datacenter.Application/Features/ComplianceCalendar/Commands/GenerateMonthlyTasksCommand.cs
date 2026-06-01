using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

/// <summary>
/// Generates all 6 compliance task types for a given company/year/month.
/// Idempotent — skips tasks that already exist.
/// </summary>
public record GenerateMonthlyTasksCommand(int ClientCompanyId, int Year, int Month)
    : IRequest<int>, IRequireCompanyAccess;
