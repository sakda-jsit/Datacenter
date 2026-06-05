using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Application.Features.Leasing.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Queries;

public class GetLeaseWorkpaperQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLeaseWorkpaperQuery, LeaseWorkpaperDto>
{
    public async Task<LeaseWorkpaperDto> Handle(GetLeaseWorkpaperQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var contracts = await db.LeaseContracts
            .AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && x.IsActive)
            .OrderBy(x => x.ContractNo)
            .ToListAsync(ct);

        // schedule closing สะสมต่อบัญชี + role (ใช้เทียบกับ GL)
        var liabilityByAcc = new Dictionary<int, decimal>();
        var deferredByAcc = new Dictionary<int, decimal>();
        var vatByAcc = new Dictionary<int, decimal>();

        var rows = new List<LeaseWorkpaperRowDto>(contracts.Count);
        foreach (var c in contracts)
        {
            var schedule = LeaseAmortizationEngine.BuildSchedule(c);
            var ye = LeaseAmortizationEngine.BuildYearEndSummary(schedule, request.FiscalYear);

            rows.Add(new LeaseWorkpaperRowDto(
                c.Id, c.ContractType, c.ContractNo, c.AssetName, c.AssetCode, c.Lessor,
                ye.GrossLiability, ye.DeferredInterest, ye.VatUndue, ye.InterestRecognizedInYear));

            Accumulate(liabilityByAcc, c.LiabilityAccountId, ye.GrossLiability.Closing);
            if (c.DeferredInterestAccountId is { } d) Accumulate(deferredByAcc, d, ye.DeferredInterest.Closing);
            if (c.InputVatUndueAccountId is { } v) Accumulate(vatByAcc, v, ye.VatUndue.Closing);
        }

        var glComparison = await BuildGlComparisonAsync(
            request.ClientCompanyId, request.FiscalYear, liabilityByAcc, deferredByAcc, vatByAcc, ct);

        return new LeaseWorkpaperDto(client.Id, client.Code, client.LegalName, request.FiscalYear, rows, glComparison);
    }

    private static void Accumulate(Dictionary<int, decimal> map, int accountId, decimal amount)
        => map[accountId] = map.GetValueOrDefault(accountId) + amount;

    /// <summary>เทียบ schedule closing รวม กับยอด GL (สะสมถึงสิ้นปีงบ) ต่อบัญชี</summary>
    private async Task<List<LeaseGlCompareDto>> BuildGlComparisonAsync(
        int clientCompanyId, int fiscalYear,
        Dictionary<int, decimal> liabilityByAcc,
        Dictionary<int, decimal> deferredByAcc,
        Dictionary<int, decimal> vatByAcc,
        CancellationToken ct)
    {
        var allAccIds = liabilityByAcc.Keys
            .Concat(deferredByAcc.Keys)
            .Concat(vatByAcc.Keys)
            .Distinct()
            .ToList();
        if (allAccIds.Count == 0) return [];

        var yearEndExclusive = new DateTime(fiscalYear, 12, 31).AddDays(1);

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => allAccIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        var glNet = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && allAccIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var result = new List<LeaseGlCompareDto>();

        // หนี้สิน: ยอดธรรมชาติเป็นเครดิต → remaining = credit − debit
        foreach (var (accId, scheduleClosing) in liabilityByAcc)
            result.Add(Compare(accId, "Liability", scheduleClosing, creditPositive: true));

        // ดอกเบี้ยรอตัด (contra) + ภาษีซื้อยังไม่ถึงกำหนด: ยอดธรรมชาติเป็นเดบิต → remaining = debit − credit
        foreach (var (accId, scheduleClosing) in deferredByAcc)
            result.Add(Compare(accId, "DeferredInterest", scheduleClosing, creditPositive: false));
        foreach (var (accId, scheduleClosing) in vatByAcc)
            result.Add(Compare(accId, "VatUndue", scheduleClosing, creditPositive: false));

        return result.OrderBy(r => r.AccountCode).ThenBy(r => r.Role).ToList();

        LeaseGlCompareDto Compare(int accId, string role, decimal scheduleClosing, bool creditPositive)
        {
            var net = glNet.GetValueOrDefault(accId);
            var debit = net?.Debit ?? 0m;
            var credit = net?.Credit ?? 0m;
            var glClosing = Math.Round(creditPositive ? credit - debit : debit - credit, 2);
            var sched = Math.Round(scheduleClosing, 2);
            var acc = accounts.GetValueOrDefault(accId);
            return new LeaseGlCompareDto(
                accId, acc?.AccountCode ?? string.Empty, acc?.AccountName ?? string.Empty,
                role, sched, glClosing, Math.Round(sched - glClosing, 2));
        }
    }
}
