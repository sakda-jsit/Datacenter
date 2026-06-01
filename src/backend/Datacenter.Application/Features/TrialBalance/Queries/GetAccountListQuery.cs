using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.TrialBalance.DTOs;
using MediatR;

namespace Datacenter.Application.Features.TrialBalance.Queries;

public record GetAccountListQuery(int ClientCompanyId, bool ActiveOnly = true)
    : IRequest<IReadOnlyList<AccountListDto>>, IRequireCompanyAccess;
