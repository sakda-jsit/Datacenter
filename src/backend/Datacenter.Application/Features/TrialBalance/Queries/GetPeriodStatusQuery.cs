using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.TrialBalance.DTOs;
using MediatR;

namespace Datacenter.Application.Features.TrialBalance.Queries;

public record GetPeriodStatusQuery(int ClientCompanyId, int Year)
    : IRequest<IReadOnlyList<PeriodStatusDto>>, IRequireCompanyAccess;
