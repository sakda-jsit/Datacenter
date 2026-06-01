using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.TrialBalance.DTOs;
using MediatR;

namespace Datacenter.Application.Features.TrialBalance.Queries;

/// <summary>
/// Computes Trial Balance for a company over a date range.
/// MonthFrom/MonthTo are optional — omit both for full-year.
/// BeginBalance = sum of journal lines BEFORE the from-date.
/// PeriodDebit/Credit = sum within the date range.
/// EndBalance = Begin + Period movement.
/// </summary>
public record GetTrialBalanceQuery(
    int ClientCompanyId,
    int Year,
    int? MonthFrom = null,
    int? MonthTo = null,
    bool IncludeZeroBalance = false)
    : IRequest<TrialBalanceReportDto>, IRequireCompanyAccess;
