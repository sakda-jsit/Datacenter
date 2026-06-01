using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

public record DeleteAccountMappingCommand(int ClientCompanyId, string AccountCode)
    : IRequest, IRequireCompanyAccess;
