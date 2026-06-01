using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// Generates Profit &amp; Loss statement for a period within a fiscal year.
/// MonthFrom/MonthTo optional — omit for full year.
/// </summary>
public record GetProfitLossQuery(
    int ClientCompanyId,
    int FiscalYear,
    int? MonthFrom = null,
    int? MonthTo = null)
    : IRequest<ProfitLossDto>, IRequireCompanyAccess;
