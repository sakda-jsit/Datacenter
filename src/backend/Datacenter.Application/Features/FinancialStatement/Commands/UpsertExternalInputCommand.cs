using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

/// <summary>
/// Save or update an external input value (currently X4 = income tax).
/// Called from Tax module when ภงด.50 amount is entered.
/// </summary>
public record UpsertExternalInputCommand(
    int ClientCompanyId,
    int FiscalYear,
    string RefCode,
    decimal Amount,
    string? Note)
    : IRequest, IRequireCompanyAccess;
