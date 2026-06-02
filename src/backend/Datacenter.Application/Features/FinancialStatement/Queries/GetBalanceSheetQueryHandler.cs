using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Application.Features.FinancialStatement.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetBalanceSheetQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBalanceSheetQuery, BalanceSheetDto>
{
    public async Task<BalanceSheetDto> Handle(GetBalanceSheetQuery request, CancellationToken ct)
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

        // Full-year date range
        var yearEnd       = new DateTime(request.FiscalYear + 1, 1, 1); // exclusive

        // Balance-sheet accounts (assets/liabilities/equity) need the CUMULATIVE balance
        // through the end of the fiscal year — opening carried-forward + in-year movement —
        // not just the year's movement. The opening journal entry is dated prior-year 12-31,
        // so a year-only range would exclude it and show only the current year's activity.
        var cumulativeNets = await GetAccountNetsAsync(db, request.ClientCompanyId,
            new DateTime(2000, 1, 1), yearEnd, ct);

        // Net of the retained-earnings account(s) at fiscal year-end, EXCLUDING the current
        // year's profit (which is added separately via netProfit below). This is the cumulative
        // balance — not just the prior-year opening — so it also captures any direct adjustments
        // booked to RE during the year (e.g. Express year-end closing entries to account 32000),
        // which otherwise leave the balance sheet out by that adjustment.
        decimal reOpeningNet = cumulativeNets
            .Where(kv => mappings.TryGetValue(kv.Key, out var m) && m.RefCode == "RE")
            .Sum(kv => kv.Value);

        // External income-tax inputs for this year: X4 = income tax expense,
        // WHT = prepaid withholding tax applied against it (balance-sheet settlement).
        var taxInputs = await db.FsExternalInputs.AsNoTracking()
            .Where(x =>
                x.ClientCompanyId == request.ClientCompanyId &&
                x.FiscalYear == request.FiscalYear &&
                (x.RefCode == "X4" || x.RefCode == "WHT"))
            .ToDictionaryAsync(x => x.RefCode, x => x.Amount, ct);
        decimal externalTax = taxInputs.GetValueOrDefault("X4");
        decimal whtApplied  = taxInputs.GetValueOrDefault("WHT");

        // Full-year net for P&L calculation (to get netProfit for RE).
        // Uses cumulative-through-year-end nets — the same basis as the standalone annual P&L —
        // so net profit (and therefore retained earnings) reconciles with the P&L report.
        var plResult = FinancialStatementEngine.BuildProfitLoss(
            client, request.FiscalYear, null, null, allLines,
            cumulativeNets, mappings, accounts, externalTax);

        return FinancialStatementEngine.BuildBalanceSheet(
            client, request.FiscalYear, allLines,
            cumulativeNets, mappings, accounts,
            reOpeningNet, plResult.NetProfit, externalTax, whtApplied);
    }

    private static async Task<Dictionary<string, decimal>> GetAccountNetsAsync(
        IApplicationDbContext db, int clientCompanyId,
        DateTime from, DateTime to, CancellationToken ct)
    {
        var lines = await db.JournalEntryLines.AsNoTracking()
            .Where(l =>
                l.JournalEntry.ClientCompanyId == clientCompanyId &&
                l.JournalEntry.JournalDate >= from &&
                l.JournalEntry.JournalDate < to)
            .Select(l => new { l.Account.AccountCode, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        return lines
            .GroupBy(l => l.AccountCode)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(l => l.DebitAmount - l.CreditAmount));
    }
}
