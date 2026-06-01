using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public record GetAccountMappingsQuery(int ClientCompanyId)
    : IRequest<IReadOnlyList<AccountMappingDto>>, IRequireCompanyAccess;
