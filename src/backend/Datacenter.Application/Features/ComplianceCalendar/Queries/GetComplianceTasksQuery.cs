using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using Datacenter.Domain.Enums;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

public record GetComplianceTasksQuery(
    int ClientCompanyId,
    int Year,
    int? Month = null,
    ComplianceTaskStatus? Status = null
) : IRequest<IReadOnlyList<ComplianceTaskDto>>, IRequireCompanyAccess;
