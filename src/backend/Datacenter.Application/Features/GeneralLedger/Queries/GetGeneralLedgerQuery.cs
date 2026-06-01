using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.GeneralLedger.DTOs;
using MediatR;

namespace Datacenter.Application.Features.GeneralLedger.Queries;

/// <summary>
/// Returns GL detail for one or all accounts of a company within a period.
/// AccountId = null means all accounts.
/// Lines are sorted by JournalDate then DocumentNo.
/// RunningBalance accumulates from OpeningBalance.
/// </summary>
public record GetGeneralLedgerQuery(
    int ClientCompanyId,
    int Year,
    int? MonthFrom = null,
    int? MonthTo = null,
    int? AccountId = null)
    : IRequest<GeneralLedgerReportDto>, IRequireCompanyAccess;
