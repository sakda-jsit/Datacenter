using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Helpers;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.GeneralLedger.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.GeneralLedger.Queries;

public class GetGeneralLedgerQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetGeneralLedgerQuery, GeneralLedgerReportDto>
{
    public async Task<GeneralLedgerReportDto> Handle(GetGeneralLedgerQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var (periodStart, periodEnd, yearStart) = PeriodRangeHelper.Get(request.Year, request.MonthFrom, request.MonthTo);

        var accountQuery = db.Accounts
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.IsActive && a.IsPostable);

        if (request.AccountId.HasValue)
            accountQuery = accountQuery.Where(a => a.Id == request.AccountId.Value);

        var accounts = await accountQuery.OrderBy(a => a.AccountCode).ToListAsync(ct);

        if (accounts.Count == 0)
            return new GeneralLedgerReportDto(
                client.Id, client.Code, client.Name,
                request.Year, request.MonthFrom, request.MonthTo, []);

        var accountIds = accounts.Select(a => a.Id).ToHashSet();

        var rawLines = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => accountIds.Contains(l.AccountId)
                     && l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                     && l.JournalEntry.JournalDate >= yearStart
                     && l.JournalEntry.JournalDate < periodEnd)
            .Select(l => new
            {
                l.AccountId,
                l.JournalEntryId,
                l.JournalEntry.DocumentNo,
                l.JournalEntry.JournalDate,
                EntryDescription = l.JournalEntry.Description,
                l.JournalEntry.SourceModule,
                LineDescription = l.Description,
                l.DebitAmount,
                l.CreditAmount,
            })
            .ToListAsync(ct);

        var glAccounts = new List<GeneralLedgerAccountDto>();

        foreach (var acc in accounts)
        {
            var accLines = rawLines.Where(l => l.AccountId == acc.Id).ToList();

            var openingLines = accLines.Where(l => l.JournalDate < periodStart).ToList();
            decimal openingBalance = openingLines.Sum(l => l.DebitAmount - l.CreditAmount);

            var periodLines = accLines
                .Where(l => l.JournalDate >= periodStart)
                .OrderBy(l => l.JournalDate)
                .ThenBy(l => l.DocumentNo)
                .ToList();

            if (periodLines.Count == 0 && openingBalance == 0)
                continue;

            decimal running = openingBalance;
            var lines = new List<GeneralLedgerLineDto>();

            foreach (var ln in periodLines)
            {
                running += ln.DebitAmount - ln.CreditAmount;
                lines.Add(new GeneralLedgerLineDto(
                    ln.JournalEntryId,
                    ln.DocumentNo,
                    ln.JournalDate,
                    ln.LineDescription ?? ln.EntryDescription,
                    ln.SourceModule,
                    ln.DebitAmount,
                    ln.CreditAmount,
                    running));
            }

            decimal totalDebit  = periodLines.Sum(l => l.DebitAmount);
            decimal totalCredit = periodLines.Sum(l => l.CreditAmount);
            decimal closing     = openingBalance + totalDebit - totalCredit;

            glAccounts.Add(new GeneralLedgerAccountDto(
                acc.Id, acc.AccountCode, acc.AccountName,
                openingBalance, totalDebit, totalCredit, closing, lines));
        }

        return new GeneralLedgerReportDto(
            client.Id, client.Code, client.Name,
            request.Year, request.MonthFrom, request.MonthTo, glAccounts);
    }
}
