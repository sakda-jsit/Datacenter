using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Adjustments.Queries;

public class GetAdjustedTrialBalanceQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdjustedTrialBalanceQuery, AdjustedTrialBalanceReportDto>
{
    public async Task<AdjustedTrialBalanceReportDto> Handle(GetAdjustedTrialBalanceQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        // ยอดสะสมถึงสิ้นปีงบ (เหมือน FS/closing): begin = ยอดยกมา OPEN-Y, movement = ยอดเคลื่อนไหว MOVE-Y.
        // ใช้ SourceModule + ช่วงปีแยก แทนการตัดด้วยวันที่อย่างเดียว เพราะ MOVE-Y (Y-12-31) กับ
        // OPEN-(Y+1) (Y-12-31) ลงวันที่เท่ากัน — กรองวันที่ล้วนจะดึง OPEN-(Y+1) มาเบิ้ลใน movement
        // (ดู FsJournalNets / fs-cumulative-double-count).
        var openingFrom = new DateTime(request.FiscalYear - 1, 1, 1);
        var yearStart   = new DateTime(request.FiscalYear, 1, 1);
        var yearEnd     = new DateTime(request.FiscalYear + 1, 1, 1);

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(ct);

        // ยอดนำเข้าจาก journal = OPEN-Y (ยอดยกมา) + MOVE-Y (เคลื่อนไหวปีนี้); IsOpening = ยอดยกมา
        var importedLines = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                     && ((l.JournalEntry.SourceModule == "OpeningBalance"
                            && l.JournalEntry.JournalDate >= openingFrom
                            && l.JournalEntry.JournalDate < yearStart)
                         || (l.JournalEntry.SourceModule != "OpeningBalance"
                            && l.JournalEntry.JournalDate >= yearStart
                            && l.JournalEntry.JournalDate < yearEnd)))
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount,
                               IsOpening = l.JournalEntry.SourceModule == "OpeningBalance" })
            .ToListAsync(ct);

        // รายการปรับปรุงของปีงบนี้
        var adjLines = await db.AdjustmentEntryLines
            .AsNoTracking()
            .Where(l => l.AdjustmentEntry.ClientCompanyId == request.ClientCompanyId
                     && l.AdjustmentEntry.FiscalYear == request.FiscalYear)
            .Select(l => new { l.AccountId, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        var importedByAcc = importedLines.ToLookup(l => l.AccountId);
        var adjByAcc      = adjLines.ToLookup(l => l.AccountId);

        var rows = new List<AdjustedTrialBalanceRowDto>();

        foreach (var acc in accounts)
        {
            var imp = importedByAcc[acc.Id].ToList();

            var beginDebit  = imp.Where(l => l.IsOpening).Sum(l => l.DebitAmount);
            var beginCredit = imp.Where(l => l.IsOpening).Sum(l => l.CreditAmount);
            var movDebit    = imp.Where(l => !l.IsOpening).Sum(l => l.DebitAmount);
            var movCredit   = imp.Where(l => !l.IsOpening).Sum(l => l.CreditAmount);

            var adj = adjByAcc[acc.Id].ToList();
            var adjDebit  = adj.Sum(l => l.DebitAmount);
            var adjCredit = adj.Sum(l => l.CreditAmount);

            // ยอดคงเหลือก่อนปรับปรุง (net-presented)
            var beforeNet = (beginDebit + movDebit) - (beginCredit + movCredit);
            var balBeforeDebit  = Math.Max(beforeNet, 0m);
            var balBeforeCredit = Math.Abs(Math.Min(beforeNet, 0m));

            // ยอดหลังปรับปรุง: Debit final = max(BalDr + AdjDr − BalCr − AdjCr, 0) (docs/13)
            var finalNet = (balBeforeDebit + adjDebit) - (balBeforeCredit + adjCredit);
            var finalDebit  = Math.Max(finalNet, 0m);
            var finalCredit = Math.Abs(Math.Min(finalNet, 0m));

            var allZero = beginDebit == 0 && beginCredit == 0 && movDebit == 0 && movCredit == 0
                       && adjDebit == 0 && adjCredit == 0;
            if (!request.IncludeZeroBalance && allZero)
                continue;

            rows.Add(new AdjustedTrialBalanceRowDto(
                acc.Id, acc.AccountCode, acc.AccountName, acc.AccountType, acc.Level, acc.ParentCode,
                beginDebit, beginCredit,
                movDebit, movCredit,
                balBeforeDebit, balBeforeCredit,
                adjDebit, adjCredit,
                finalDebit, finalCredit));
        }

        return new AdjustedTrialBalanceReportDto(
            client.Id, client.Code, client.LegalName, request.FiscalYear, rows);
    }
}
