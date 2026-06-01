using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// Generates Balance Sheet as of fiscal year-end.
/// Uses full-year trial balance (Jan–Dec or full fiscal year).
/// RE line is computed as: -(prior-year RE opening) + current-year net profit.
/// </summary>
public record GetBalanceSheetQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<BalanceSheetDto>, IRequireCompanyAccess;
