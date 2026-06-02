using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using MediatR;

namespace Datacenter.Application.Features.ClosingPeriod.Queries;

public record GetClosingPeriodsQuery(int ClientCompanyId, int Year)
    : IRequest<ClosingPeriodOverviewDto>, IRequireCompanyAccess;
