using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Helpers;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Application.Features.FinancialStatement.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetProfitLossQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProfitLossQuery, ProfitLossDto>
{
    public async Task<ProfitLossDto> Handle(GetProfitLossQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var allLines = await db.StatementLines.AsNoTracking()
            .OrderBy(l => l.SortOrder).ToListAsync(ct);

        var mappings = await db.AccountStatementMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .ToDictionaryAsync(m => m.AccountCode, ct);

        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.IsActive)
            .ToDictionaryAsync(a => a.AccountCode, ct);

        var (periodStart, periodEnd, _) = PeriodRangeHelper.Get(
            request.FiscalYear, request.MonthFrom, request.MonthTo);

        // Express exports annual figures as an opening snapshot OPEN-{fy} (the brought-forward
        // trial balance, dated (fy-1)-12-31) plus the year's movement MOVE-{fy} (dated fy-12-31),
        // where the fiscal-year total = OPEN-{fy} + MOVE-{fy}. For the annual report we therefore
        // take exactly that year's opening snapshot + that year's movement — sharing the balance
        // sheet's selection logic so that a later year's restated opening (OPEN-{fy+1}, dated
        // within this period) is NOT re-added, which would otherwise inflate income/expense.
        // A specific month range stays a best-effort in-period slice (see known limitation:
        // monthly figures are not accurate because movement is lumped at year-end).
        bool isAnnual = request.MonthFrom is null && request.MonthTo is null;

        Dictionary<string, decimal> accountNets;
        if (isAnnual)
        {
            accountNets = await FsJournalNets.CumulativeAsync(
                db, request.ClientCompanyId, request.FiscalYear, ct);
        }
        else
        {
            var periodNets = await db.JournalEntryLines.AsNoTracking()
                .Where(l =>
                    l.JournalEntry.ClientCompanyId == request.ClientCompanyId &&
                    l.JournalEntry.JournalDate >= periodStart &&
                    l.JournalEntry.JournalDate < periodEnd)
                .Select(l => new { l.Account.AccountCode, l.DebitAmount, l.CreditAmount })
                .ToListAsync(ct);

            accountNets = periodNets
                .GroupBy(l => l.AccountCode)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.DebitAmount - l.CreditAmount));
        }

        var x4Input = await db.FsExternalInputs.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ClientCompanyId == request.ClientCompanyId &&
                x.FiscalYear == request.FiscalYear &&
                x.RefCode == "X4", ct);
        decimal externalTax = x4Input?.Amount ?? 0m;

        return FinancialStatementEngine.BuildProfitLoss(
            client, request.FiscalYear, request.MonthFrom, request.MonthTo,
            allLines, accountNets, mappings, accounts, externalTax);
    }
}
