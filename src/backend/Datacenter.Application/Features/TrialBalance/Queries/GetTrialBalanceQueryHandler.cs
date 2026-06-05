using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Helpers;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.TrialBalance.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.TrialBalance.Queries;

public class GetTrialBalanceQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTrialBalanceQuery, TrialBalanceReportDto>
{
    public async Task<TrialBalanceReportDto> Handle(GetTrialBalanceQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var (periodStart, periodEnd, yearStart) = PeriodRangeHelper.Get(request.Year, request.MonthFrom, request.MonthTo);

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(ct);

        var allLines = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                     && l.JournalEntry.JournalDate >= yearStart
                     && l.JournalEntry.JournalDate < periodEnd)
            .Select(l => new
            {
                l.AccountId,
                l.DebitAmount,
                l.CreditAmount,
                l.JournalEntry.JournalDate,
            })
            .ToListAsync(ct);

        var rows = new List<TrialBalanceRowDto>();

        foreach (var acc in accounts)
        {
            var accLines = allLines.Where(l => l.AccountId == acc.Id).ToList();

            var beginLines = accLines.Where(l => l.JournalDate < periodStart).ToList();
            decimal beginDebit  = beginLines.Sum(l => l.DebitAmount);
            decimal beginCredit = beginLines.Sum(l => l.CreditAmount);

            var periodLines = accLines.Where(l => l.JournalDate >= periodStart).ToList();
            decimal periodDebit  = periodLines.Sum(l => l.DebitAmount);
            decimal periodCredit = periodLines.Sum(l => l.CreditAmount);

            decimal endDebit  = beginDebit  + periodDebit;
            decimal endCredit = beginCredit + periodCredit;

            if (!request.IncludeZeroBalance
                && beginDebit == 0 && beginCredit == 0
                && periodDebit == 0 && periodCredit == 0)
                continue;

            rows.Add(new TrialBalanceRowDto(
                acc.Id, acc.AccountCode, acc.AccountName, acc.AccountType,
                acc.Level, acc.ParentCode,
                beginDebit, beginCredit,
                periodDebit, periodCredit,
                endDebit, endCredit));
        }

        return new TrialBalanceReportDto(
            client.Id, client.Code, client.LegalName,
            request.Year, request.MonthFrom, request.MonthTo,
            rows);
    }
}
