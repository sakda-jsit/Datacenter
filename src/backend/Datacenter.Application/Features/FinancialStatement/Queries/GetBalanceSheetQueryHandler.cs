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
        var yearStart     = new DateTime(request.FiscalYear, 1, 1);
        var yearEnd       = new DateTime(request.FiscalYear + 1, 1, 1); // exclusive
        var priorYearEnd  = yearStart; // opening = everything before year start

        // Aggregate journal lines: net = debit - credit per account, for the full year
        var yearNets = await GetAccountNetsAsync(db, request.ClientCompanyId, yearStart, yearEnd, ct);

        // Opening balances (prior year ending = current year opening)
        var openingNets = await GetAccountNetsAsync(db, request.ClientCompanyId,
            new DateTime(2000, 1, 1), priorYearEnd, ct);

        // RE opening: net of accounts mapped to "RE" before this year
        decimal reOpeningNet = openingNets
            .Where(kv => mappings.TryGetValue(kv.Key, out var m) && m.RefCode == "RE")
            .Sum(kv => kv.Value);

        // Get external tax for this year (X4)
        var x4Input = await db.FsExternalInputs.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ClientCompanyId == request.ClientCompanyId &&
                x.FiscalYear == request.FiscalYear &&
                x.RefCode == "X4", ct);
        decimal externalTax = x4Input?.Amount ?? 0m;

        // Full-year net for P&L calculation (to get netProfit for RE)
        var plResult = FinancialStatementEngine.BuildProfitLoss(
            client, request.FiscalYear, null, null, allLines,
            yearNets, mappings, accounts, externalTax);

        return FinancialStatementEngine.BuildBalanceSheet(
            client, request.FiscalYear, allLines,
            yearNets, mappings, accounts,
            reOpeningNet, plResult.NetProfit);
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
