using Datacenter.Application.Common.Exceptions;
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

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.IsActive)
            .OrderBy(a => a.AccountCode)
            .ToListAsync(ct);

        // กรองด้วยปีงบ (explicit) แล้วแยก ยอดยกมา (OPEN-Y) / เคลื่อนไหว (MOVE-Y) ด้วย SourceModule.
        // begin = OPEN-Y (เดิม bug: ตัด OpeningBalance ทิ้งหมด → ยอดยกมาเป็น 0 เสมอ — แก้แล้ว)
        var lines = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                     && l.JournalEntry.FiscalYear == request.Year)
            .Select(l => new
            {
                l.AccountId,
                l.DebitAmount,
                l.CreditAmount,
                IsOpening = l.JournalEntry.SourceModule == "OpeningBalance",
            })
            .ToListAsync(ct);

        // Express ลงยอดเคลื่อนไหวทั้งปีเป็นก้อนเดียวลงวันที่ 31/12 → ไม่มีความละเอียดรายเดือนจริง.
        // มุมมองเต็มปี (หรือช่วงครอบ ธ.ค.) แสดงเคลื่อนไหว; ช่วงที่ไม่ถึง ธ.ค. = ยังไม่มีเคลื่อนไหว (0).
        int mTo = request.MonthTo ?? 12;
        bool includeMovement = (request.MonthFrom is null && request.MonthTo is null) || mTo >= 12;

        var byAcc = lines.ToLookup(l => l.AccountId);
        var rows = new List<TrialBalanceRowDto>();

        foreach (var acc in accounts)
        {
            var accLines = byAcc[acc.Id];

            decimal beginDebit  = accLines.Where(l => l.IsOpening).Sum(l => l.DebitAmount);
            decimal beginCredit = accLines.Where(l => l.IsOpening).Sum(l => l.CreditAmount);

            decimal periodDebit  = includeMovement ? accLines.Where(l => !l.IsOpening).Sum(l => l.DebitAmount)  : 0m;
            decimal periodCredit = includeMovement ? accLines.Where(l => !l.IsOpening).Sum(l => l.CreditAmount) : 0m;

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
