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

        // Balance-sheet accounts (assets/liabilities/equity) need the CUMULATIVE balance
        // through the end of the fiscal year — opening carried-forward + in-year movement.
        // The Express importer (ExpressPostingService) posts, per fiscal year Y, an opening
        // snapshot OPEN-Y (SourceModule "OpeningBalance", dated (Y-1)-12-31, = the full
        // brought-forward trial balance) plus a movement entry MOVE-Y (dated Y-12-31), where
        // closing(Y) = OPEN-Y + MOVE-Y. The year-end balance is therefore EXACTLY that year's
        // opening snapshot plus that year's movement — NOT the sum of every entry since
        // inception, which re-adds each later year's full opening snapshot (e.g. OPEN-(Y+1),
        // which is itself a restated closing balance) and inflates the balance sheet ≈2× once
        // a company has two years of data posted.
        // หลังปรับปรุง: รวม AJE ปิดงบใน-ระบบ (AdjustmentEntry) ของปีนี้ด้วย — งบที่ยื่น = งบหลังปรับปรุง.
        // การกระจาย: บัญชี BS → asset/liab/equity line; บัญชี P&L → netProfit → RE; บัญชี RE ตรง → reOpeningNet.
        var cumulativeNets = await FsJournalNets.CumulativeWithAdjustmentsAsync(db, request.ClientCompanyId,
            request.FiscalYear, ct);

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

}
