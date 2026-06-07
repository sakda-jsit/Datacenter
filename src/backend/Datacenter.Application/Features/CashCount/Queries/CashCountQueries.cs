using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CashCount.DTOs;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CashCount.Queries;

// ── List ────────────────────────────────────────────────────────────────────────
public record GetCashCountsQuery(int ClientCompanyId, int? FiscalYear = null, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<CashCountListItemDto>>, IRequireCompanyAccess;

public class GetCashCountsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCashCountsQuery, IReadOnlyList<CashCountListItemDto>>
{
    public async Task<IReadOnlyList<CashCountListItemDto>> Handle(GetCashCountsQuery request, CancellationToken ct)
    {
        var items = await db.CashCounts.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId
                     && (request.IncludeInactive || x.IsActive)
                     && (request.FiscalYear == null || x.FiscalYear == request.FiscalYear))
            .OrderByDescending(x => x.CountDate).ThenBy(x => x.Reference)
            .ToListAsync(ct);

        var accIds = items.Select(x => x.CashAccountId).Distinct().ToList();
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        return items.Select(e => CashCountMapper.ToListItem(e, accounts)).ToList();
    }
}

// ── Detail ──────────────────────────────────────────────────────────────────────
public record GetCashCountQuery(int Id, int ClientCompanyId)
    : IRequest<CashCountDto>, IRequireCompanyAccess;

public class GetCashCountQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCashCountQuery, CashCountDto>
{
    public async Task<CashCountDto> Handle(GetCashCountQuery request, CancellationToken ct)
    {
        var entity = await db.CashCounts.AsNoTracking().Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("CashCount", request.Id);

        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == entity.CashAccountId).ToDictionaryAsync(a => a.Id, ct);
        return CashCountMapper.ToDto(entity, accounts);
    }
}

// ── Workpaper + GL compare ────────────────────────────────────────────────────────
public record GetCashCountWorkpaperQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<CashCountWorkpaperDto>, IRequireCompanyAccess;

public class GetCashCountWorkpaperQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCashCountWorkpaperQuery, CashCountWorkpaperDto>
{
    public async Task<CashCountWorkpaperDto> Handle(GetCashCountWorkpaperQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var items = await db.CashCounts.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && x.IsActive && x.FiscalYear == request.FiscalYear)
            .OrderByDescending(x => x.CountDate).ThenBy(x => x.Reference)
            .ToListAsync(ct);

        var accIds = items.Select(x => x.CashAccountId).Distinct().ToList();
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

        var countedByAcc = new Dictionary<int, decimal>();
        var rows = new List<CashCountWorkpaperRowDto>(items.Count);
        foreach (var e in items)
        {
            var total = CashCountMapper.CountedTotal(e);
            var acc = accounts.GetValueOrDefault(e.CashAccountId);
            rows.Add(new CashCountWorkpaperRowDto(
                e.Id, e.CountDate, e.Reference, e.CashAccountId, acc?.AccountCode, acc?.AccountName, total));
            countedByAcc[e.CashAccountId] = countedByAcc.GetValueOrDefault(e.CashAccountId) + total;
        }

        var glComparison = await BuildGlComparisonAsync(request.ClientCompanyId, request.FiscalYear, countedByAcc, accounts, ct);
        return new CashCountWorkpaperDto(client.Id, client.Code, client.LegalName, request.FiscalYear, rows, glComparison);
    }

    /// <summary>เทียบยอดนับจริง (รวมตามบัญชี) กับยอด GL บัญชีเงินสด (debit − credit) สะสมถึงสิ้นปีงบ</summary>
    private async Task<List<CashCountGlCompareDto>> BuildGlComparisonAsync(
        int clientCompanyId, int fiscalYear, Dictionary<int, decimal> countedByAcc,
        IReadOnlyDictionary<int, Account> accounts, CancellationToken ct)
    {
        if (countedByAcc.Count == 0) return [];
        var accIds = countedByAcc.Keys.ToList();
        var yearEndExclusive = new DateTime(fiscalYear, 12, 31).AddDays(1);

        var glNet = await db.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && accIds.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var result = new List<CashCountGlCompareDto>();
        foreach (var (accId, counted) in countedByAcc)
        {
            var net = glNet.GetValueOrDefault(accId);
            var glClosing = Math.Round((net?.Debit ?? 0m) - (net?.Credit ?? 0m), 2);
            var c = Math.Round(counted, 2);
            var acc = accounts.GetValueOrDefault(accId);
            result.Add(new CashCountGlCompareDto(
                accId, acc?.AccountCode ?? string.Empty, acc?.AccountName ?? string.Empty,
                c, glClosing, Math.Round(c - glClosing, 2)));
        }
        return result.OrderBy(r => r.AccountCode).ToList();
    }
}
