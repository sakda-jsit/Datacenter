using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using MediatR;

namespace Datacenter.Application.Features.ClosingPeriod.Queries;

public record GetClosingValidationQuery(int ClientCompanyId, int Year, int Month)
    : IRequest<ClosingValidationDto>, IRequireCompanyAccess;
