using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Application.Features.FixedAssets.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Queries;

public class GetFixedAssetWorkpaperQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFixedAssetWorkpaperQuery, FixedAssetWorkpaperDto>
{
    public async Task<FixedAssetWorkpaperDto> Handle(GetFixedAssetWorkpaperQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var assets = await db.FixedAssets
            .AsNoTracking()
            .Include(x => x.AssetType)
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && x.IsActive
                     && x.AcquireDate.Year <= request.FiscalYear)
            .OrderBy(x => x.AssetCode)
            .ToListAsync(ct);

        // ชื่อประเภทสำหรับ NOTE2: ใช้ AssetType (ถ้า map แล้ว) → คำอธิบายหมวดจาก mapping (ACCCOD) → รหัสหมวด
        var mappingDesc = await db.AssetAccountMappings
            .AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId && m.Description != null)
            .ToDictionaryAsync(m => m.CategoryCode.ToUpper(), m => m.Description!, ct);

        string TypeLabel(Domain.Entities.FixedAsset a)
        {
            if (!string.IsNullOrWhiteSpace(a.AssetType?.Name)) return a.AssetType!.Name;
            if (!string.IsNullOrWhiteSpace(a.CategoryCode))
            {
                if (mappingDesc.TryGetValue(a.CategoryCode!.ToUpper(), out var desc)) return desc;
                return a.CategoryCode!;
            }
            return "(ไม่ระบุประเภท)";
        }

        // accum dep สะสมสิ้นปี + ค่าเสื่อมงวด (ชุดบัญชี) ต่อบัญชี → ใช้เทียบ GL
        var accumByAcc = new Dictionary<int, decimal>();
        var expenseByAcc = new Dictionary<int, decimal>();

        var rows = new List<FixedAssetWorkpaperRowDto>(assets.Count);
        var summaryAgg = new Dictionary<string, (int Count, decimal Cost, decimal Accum, decimal Nbv, decimal Charge)>();
        foreach (var a in assets)
        {
            var book = DepreciationEngine.AsOf(a, a.BookRatePct, request.FiscalYear);
            var tax = DepreciationEngine.AsOf(a, a.TaxRatePct, request.FiscalYear);
            var disposal = DepreciationEngine.Disposal(a);

            rows.Add(new FixedAssetWorkpaperRowDto(
                a.Id, a.AssetCode, a.AssetName, TypeLabel(a), a.AcquireDate, a.Cost, a.Status,
                book, tax, disposal));

            Accumulate(accumByAcc, a.AccumDepreciationAccountId, book.ClosingAccumulated);
            if (book.Charge != 0)
                Accumulate(expenseByAcc, a.DepreciationExpenseAccountId, book.Charge);

            var label = TypeLabel(a);
            var cur = summaryAgg.GetValueOrDefault(label);
            summaryAgg[label] = (cur.Count + 1, cur.Cost + a.Cost,
                cur.Accum + book.ClosingAccumulated, cur.Nbv + book.NetBookValue, cur.Charge + book.Charge);
        }

        var typeSummary = summaryAgg
            .Select(kv => new FixedAssetTypeSummaryDto(
                kv.Key, kv.Value.Count,
                Math.Round(kv.Value.Cost, 2), Math.Round(kv.Value.Accum, 2),
                Math.Round(kv.Value.Nbv, 2), Math.Round(kv.Value.Charge, 2)))
            .OrderBy(s => s.AssetTypeName)
            .ToList();

        var glComparison = await BuildGlComparisonAsync(
            request.ClientCompanyId, request.FiscalYear, accumByAcc, expenseByAcc, ct);

        return new FixedAssetWorkpaperDto(
            client.Id, client.Code, client.LegalName, request.FiscalYear, rows, typeSummary, glComparison);
    }

    private static void Accumulate(Dictionary<int, decimal> map, int accountId, decimal amount)
        => map[accountId] = map.GetValueOrDefault(accountId) + amount;

    /// <summary>
    /// เทียบ schedule (ชุดบัญชี) กับ GL:
    /// - ค่าเสื่อมสะสม (contra-asset, เครดิต): ยอดสะสมสิ้นปี เทียบ GL สะสมถึงสิ้นปี (credit − debit)
    /// - ค่าเสื่อมราคา (P&amp;L): ค่าเสื่อมงวดปีนี้ เทียบ movement ในปี (debit − credit ระหว่างปีงบ)
    /// </summary>
    private async Task<List<FixedAssetGlCompareDto>> BuildGlComparisonAsync(
        int clientCompanyId, int fiscalYear,
        Dictionary<int, decimal> accumByAcc,
        Dictionary<int, decimal> expenseByAcc,
        CancellationToken ct)
    {
        var allAccIds = accumByAcc.Keys.Concat(expenseByAcc.Keys).Distinct().ToList();
        if (allAccIds.Count == 0) return [];

        var yearStart = new DateTime(fiscalYear, 1, 1);
        var yearEndExclusive = new DateTime(fiscalYear, 12, 31).AddDays(1);

        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => allAccIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        // accum dep: สะสมถึงสิ้นปี
        var glCumulative = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && accumByAcc.Keys.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        // dep expense: movement ในปีงบ
        var glMovement = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate >= yearStart
                     && l.JournalEntry.JournalDate < yearEndExclusive
                     && expenseByAcc.Keys.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var result = new List<FixedAssetGlCompareDto>();

        foreach (var (accId, scheduleAmount) in accumByAcc)
        {
            var net = glCumulative.GetValueOrDefault(accId);
            var glAmount = Math.Round((net?.Credit ?? 0m) - (net?.Debit ?? 0m), 2);
            result.Add(Build(accId, "AccumDepreciation", scheduleAmount, glAmount));
        }

        foreach (var (accId, scheduleAmount) in expenseByAcc)
        {
            var net = glMovement.GetValueOrDefault(accId);
            var glAmount = Math.Round((net?.Debit ?? 0m) - (net?.Credit ?? 0m), 2);
            result.Add(Build(accId, "DepreciationExpense", scheduleAmount, glAmount));
        }

        return result.OrderBy(r => r.AccountCode).ThenBy(r => r.Role).ToList();

        FixedAssetGlCompareDto Build(int accId, string role, decimal scheduleAmount, decimal glAmount)
        {
            var acc = accounts.GetValueOrDefault(accId);
            var sched = Math.Round(scheduleAmount, 2);
            return new FixedAssetGlCompareDto(
                accId, acc?.AccountCode ?? string.Empty, acc?.AccountName ?? string.Empty,
                role, sched, glAmount, Math.Round(sched - glAmount, 2));
        }
    }
}
