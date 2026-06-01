using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

public record GetComplianceDashboardQuery(int ClientCompanyId, int Year)
    : IRequest<ComplianceDashboardDto>, IRequireCompanyAccess;
