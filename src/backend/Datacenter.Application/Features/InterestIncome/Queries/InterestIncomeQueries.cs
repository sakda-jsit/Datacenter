using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.InterestIncome.DTOs;
using Datacenter.Application.Features.InterestIncome.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.InterestIncome.Queries;

// ── List ────────────────────────────────────────────────────────────────────────
public record GetInterestLoansQuery(int ClientCompanyId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<InterestLoanListItemDto>>, IRequireCompanyAccess;

public class GetInterestLoansQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInterestLoansQuery, IReadOnlyList<InterestLoanListItemDto>>
{
    public async Task<IReadOnlyList<InterestLoanListItemDto>> Handle(GetInterestLoansQuery request, CancellationToken ct)
    {
        var items = await db.InterestBearingLoans.AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || x.IsActive))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return items.Select(InterestLoanMapper.ToListItem).ToList();
    }
}

// ── Detail + segments ─────────────────────────────────────────────────────────────
public record GetInterestLoanQuery(int Id, int ClientCompanyId, int FiscalYear)
    : IRequest<InterestLoanDetailDto>, IRequireCompanyAccess;

public class GetInterestLoanQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInterestLoanQuery, InterestLoanDetailDto>
{
    public async Task<InterestLoanDetailDto> Handle(GetInterestLoanQuery request, CancellationToken ct)
    {
        var entity = await db.InterestBearingLoans.AsNoTracking().Include(x => x.Movements)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("InterestBearingLoan", request.Id);

        var accIds = new[] { entity.InterestReceivableAccountId, entity.InterestIncomeAccountId }.Distinct().ToList();
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        var segments = InterestIncomeEngine.BuildSegments(entity, request.FiscalYear);
        var asOf = InterestIncomeEngine.AsOf(entity, request.FiscalYear);
        return new InterestLoanDetailDto(InterestLoanMapper.ToDto(entity, accounts), asOf, segments);
    }
}

// ── Workpaper + GL compare ────────────────────────────────────────────────────────
public record GetInterestWorkpaperQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<InterestWorkpaperDto>, IRequireCompanyAccess;

public class GetInterestWorkpaperQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInterestWorkpaperQuery, InterestWorkpaperDto>
{
    public async Task<InterestWorkpaperDto> Handle(GetInterestWorkpaperQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var items = await db.InterestBearingLoans.AsNoTracking().Include(x => x.Movements)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        var interestByAcc = new Dictionary<int, decimal>();
        var rows = new List<InterestWorkpaperRowDto>(items.Count);
        foreach (var loan in items)
        {
            var a = InterestIncomeEngine.AsOf(loan, request.FiscalYear);
            rows.Add(new InterestWorkpaperRowDto(
                loan.Id, loan.Name, loan.Reference, loan.AnnualRatePct,
                a.OpeningBalance, a.ClosingBalance, a.InterestForYear, a.Sbt, a.LocalTax));
            interestByAcc[loan.InterestIncomeAccountId] = interestByAcc.GetValueOrDefault(loan.InterestIncomeAccountId) + a.InterestForYear;
        }

        var glComparison = await BuildGlComparisonAsync(request.ClientCompanyId, request.FiscalYear, interestByAcc, ct);
        return new InterestWorkpaperDto(client.Id, client.Code, client.LegalName, request.FiscalYear, rows, glComparison);
    }

    /// <summary>เทียบดอกเบี้ยที่คำนวณ กับ movement บัญชีรายได้ดอกเบี้ยใน GL ในปีงบ (credit − debit; รายได้ = credit-positive)</summary>
    private async Task<List<InterestGlCompareDto>> BuildGlComparisonAsync(
        int clientCompanyId, int fiscalYear, Dictionary<int, decimal> interestByAcc, CancellationToken ct)
    {
        var accIds = interestByAcc.Keys.ToList();
        if (accIds.Count == 0) return [];

        var yearStart = new DateTime(fiscalYear, 1, 1);
        var yearEndExclusive = new DateTime(fiscalYear, 12, 31).AddDays(1);
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        var glNet = await db.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate >= yearStart
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && accIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var result = new List<InterestGlCompareDto>();
        foreach (var (accId, scheduleInterest) in interestByAcc)
        {
            var net = glNet.GetValueOrDefault(accId);
            var glMovement = Math.Round((net?.Credit ?? 0m) - (net?.Debit ?? 0m), 2); // income = credit-positive
            var sched = Math.Round(scheduleInterest, 2);
            var acc = accounts.GetValueOrDefault(accId);
            result.Add(new InterestGlCompareDto(
                accId, acc?.AccountCode ?? string.Empty, acc?.AccountName ?? string.Empty,
                sched, glMovement, Math.Round(sched - glMovement, 2)));
        }
        return result.OrderBy(r => r.AccountCode).ToList();
    }
}
