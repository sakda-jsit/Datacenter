using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

/// <summary>Create or update a single account→RefCode mapping.</summary>
public record UpsertAccountMappingCommand(
    int ClientCompanyId,
    string AccountCode,
    string AccountName,
    string RefCode)
    : IRequest, IRequireCompanyAccess;
