using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using MediatR;

namespace Datacenter.Application.Features.ClosingPeriod.Commands;

public record CloseClosingPeriodCommand(int ClientCompanyId, int Year, int Month)
    : IRequest<ClosingPeriodMonthDto>, IRequireCompanyAccess;
