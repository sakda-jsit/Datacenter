using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Prepaid.DTOs;
using Datacenter.Application.Features.Prepaid.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Prepaid.Queries;

// ── List ────────────────────────────────────────────────────────────────────────
public record GetPrepaidExpensesQuery(int ClientCompanyId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<PrepaidListItemDto>>, IRequireCompanyAccess;

public class GetPrepaidExpensesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPrepaidExpensesQuery, IReadOnlyList<PrepaidListItemDto>>
{
    public async Task<IReadOnlyList<PrepaidListItemDto>> Handle(GetPrepaidExpensesQuery request, CancellationToken ct)
    {
        var items = await db.PrepaidExpenses.AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || x.IsActive))
            .OrderBy(x => x.StartDate).ThenBy(x => x.Name)
            .ToListAsync(ct);
        return items.Select(PrepaidMapper.ToListItem).ToList();
    }
}

// ── Detail + schedule ─────────────────────────────────────────────────────────────
public record GetPrepaidExpenseQuery(int Id, int ClientCompanyId, int FiscalYear)
    : IRequest<PrepaidDetailDto>, IRequireCompanyAccess;

public class GetPrepaidExpenseQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPrepaidExpenseQuery, PrepaidDetailDto>
{
    public async Task<PrepaidDetailDto> Handle(GetPrepaidExpenseQuery request, CancellationToken ct)
    {
        var entity = await db.PrepaidExpenses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PrepaidExpense", request.Id);

        var accIds = new[] { entity.PrepaidAccountId, entity.ExpenseAccountId }.Distinct().ToList();
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        var schedule = PrepaidAmortizationEngine.BuildSchedule(entity);
        var asOf = PrepaidAmortizationEngine.AsOf(entity, request.FiscalYear);
        return new PrepaidDetailDto(PrepaidMapper.ToDto(entity, accounts), asOf, schedule);
    }
}

// ── Workpaper + GL compare ────────────────────────────────────────────────────────
public record GetPrepaidWorkpaperQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<PrepaidWorkpaperDto>, IRequireCompanyAccess;

public class GetPrepaidWorkpaperQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPrepaidWorkpaperQuery, PrepaidWorkpaperDto>
{
    public async Task<PrepaidWorkpaperDto> Handle(GetPrepaidWorkpaperQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var items = await db.PrepaidExpenses.AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && x.IsActive)
            .OrderBy(x => x.StartDate).ThenBy(x => x.Name)
            .ToListAsync(ct);

        var remainingByAcc = new Dictionary<int, decimal>();
        var rows = new List<PrepaidWorkpaperRowDto>(items.Count);
        foreach (var p in items)
        {
            var a = PrepaidAmortizationEngine.AsOf(p, request.FiscalYear);
            rows.Add(new PrepaidWorkpaperRowDto(
                p.Id, p.Code, p.Name, p.Reference, p.TotalAmount, p.StartDate, p.EndDate,
                a.OpeningAmortized, a.Charge, a.ClosingAmortized, a.Remaining));
            remainingByAcc[p.PrepaidAccountId] = remainingByAcc.GetValueOrDefault(p.PrepaidAccountId) + a.Remaining;
        }

        var glComparison = await BuildGlComparisonAsync(request.ClientCompanyId, request.FiscalYear, remainingByAcc, ct);
        return new PrepaidWorkpaperDto(client.Id, client.Code, client.LegalName, request.FiscalYear, rows, glComparison);
    }

    /// <summary>เทียบยอดคงเหลือตาม schedule กับยอด GL บัญชีค่าใช้จ่ายจ่ายล่วงหน้า (debit-positive) สะสมถึงสิ้นปีงบ</summary>
    private async Task<List<PrepaidGlCompareDto>> BuildGlComparisonAsync(
        int clientCompanyId, int fiscalYear, Dictionary<int, decimal> remainingByAcc, CancellationToken ct)
    {
        var accIds = remainingByAcc.Keys.ToList();
        if (accIds.Count == 0) return [];

        var yearEndExclusive = new DateTime(fiscalYear, 12, 31).AddDays(1);
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        var glNet = await db.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && accIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var result = new List<PrepaidGlCompareDto>();
        foreach (var (accId, scheduleRemaining) in remainingByAcc)
        {
            var net = glNet.GetValueOrDefault(accId);
            var glClosing = Math.Round((net?.Debit ?? 0m) - (net?.Credit ?? 0m), 2);
            var sched = Math.Round(scheduleRemaining, 2);
            var acc = accounts.GetValueOrDefault(accId);
            result.Add(new PrepaidGlCompareDto(
                accId, acc?.AccountCode ?? string.Empty, acc?.AccountName ?? string.Empty,
                sched, glClosing, Math.Round(sched - glClosing, 2)));
        }
        return result.OrderBy(r => r.AccountCode).ToList();
    }
}
