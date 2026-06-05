using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Application.Features.FinancialStatement.Services;
using Datacenter.Application.Features.FixedAssets.Services;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetNotesToFsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetNotesToFsQuery, NotesToFsDto>
{
    public async Task<NotesToFsDto> Handle(GetNotesToFsQuery request, CancellationToken ct)
    {
        int year = request.FiscalYear;
        int prior = year - 1;

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

        // ── ยอดบัญชีสะสมถึงสิ้นปี (cumulative) — ฐานเดียวกับ GetProfitLossQueryHandler ──
        // ทั้งงบฐานะและงบกำไรขาดทุนใช้ยอดสะสมถึงสิ้นปี เพื่อให้หมายเหตุ "ลงตรง" กับงบที่แสดง.
        var epoch = new DateTime(2000, 1, 1);
        var nCurrent  = await NetsAsync(request.ClientCompanyId, epoch, YearEndExcl(year),      ct);
        var nPrior    = await NetsAsync(request.ClientCompanyId, epoch, YearEndExcl(prior),     ct);
        var nTwoPrior = await NetsAsync(request.ClientCompanyId, epoch, YearEndExcl(prior - 1), ct);

        var schedules = NotesEngine.BuildSchedules(
            allLines, mappings, accounts, nCurrent, nPrior);

        var costOfSales = NotesEngine.BuildCostOfSales(
            allLines, mappings, accounts, nCurrent, nPrior, nTwoPrior);

        // ── ตารางการเคลื่อนไหว 6.6 (ที่ดิน อาคาร อุปกรณ์) + 6.7 (สินทรัพย์ไม่มีตัวตน) ──
        var movements = await BuildMovementsAsync(request.ClientCompanyId, year, prior, ct);

        // ── ข้อความ template (เลือก EffectiveYear ≤ ปีงบ; บริษัท override default กลาง) ──
        var narratives = await BuildNarrativesAsync(request.ClientCompanyId, year, prior, client, ct);

        int yearTh = year + 543;
        return new NotesToFsDto(
            client.Id, client.LegalName, client.TaxId, client.Address,
            year, prior,
            $"สำหรับปีสิ้นสุดวันที่ 31 ธันวาคม {yearTh}",
            narratives, schedules, movements, costOfSales);
    }

    private static DateTime YearEndExcl(int y)  => new(y + 1, 1, 1);

    private async Task<Dictionary<string, decimal>> NetsAsync(
        int clientCompanyId, DateTime from, DateTime toExcl, CancellationToken ct)
    {
        var lines = await db.JournalEntryLines.AsNoTracking()
            .Where(l =>
                l.JournalEntry.ClientCompanyId == clientCompanyId &&
                l.JournalEntry.JournalDate >= from &&
                l.JournalEntry.JournalDate < toExcl)
            .Select(l => new { l.Account.AccountCode, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        return lines
            .GroupBy(l => l.AccountCode)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.DebitAmount - l.CreditAmount));
    }

    // ── Movement tables (PP&E / intangible) จาก FA register ──────────────────────

    private async Task<List<NoteMovementDto>> BuildMovementsAsync(
        int clientCompanyId, int year, int prior, CancellationToken ct)
    {
        var assets = await db.FixedAssets.AsNoTracking()
            .Include(x => x.AssetType)
            .Where(x => x.ClientCompanyId == clientCompanyId && x.IsActive && x.AcquireDate.Year <= year)
            .ToListAsync(ct);
        if (assets.Count == 0) return [];

        var mappingDesc = await db.AssetAccountMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == clientCompanyId && m.Description != null)
            .ToDictionaryAsync(m => m.CategoryCode.ToUpper(), m => m.Description!, ct);

        string Label(FixedAsset a)
        {
            if (!string.IsNullOrWhiteSpace(a.AssetType?.Name)) return a.AssetType!.Name;
            if (!string.IsNullOrWhiteSpace(a.CategoryCode))
                return mappingDesc.TryGetValue(a.CategoryCode!.ToUpper(), out var d) ? d : a.CategoryCode!;
            return "(ไม่ระบุประเภท)";
        }

        // แยกสินทรัพย์ไม่มีตัวตนออกจาก ที่ดิน อาคาร อุปกรณ์ ด้วยชื่อประเภท
        static bool IsIntangible(string label)
        {
            var l = label.ToLowerInvariant();
            return l.Contains("ไม่มีตัวตน") || l.Contains("โปรแกรม") || l.Contains("ซอฟต์แวร์")
                || l.Contains("software") || l.Contains("ลิขสิทธิ์") || l.Contains("สิทธิบัตร");
        }

        var ppe = assets.Where(a => !IsIntangible(Label(a))).ToList();
        var intangible = assets.Where(a => IsIntangible(Label(a))).ToList();

        var result = new List<NoteMovementDto>();
        var ppeNote = BuildMovement("6.6", "ที่ดิน อาคารและอุปกรณ์", 66, ppe, year, prior, Label);
        if (ppeNote != null) result.Add(ppeNote);
        var intNote = BuildMovement("6.7", "สินทรัพย์ไม่มีตัวตน", 67, intangible, year, prior, Label);
        if (intNote != null) result.Add(intNote);
        return result;
    }

    private static NoteMovementDto? BuildMovement(
        string no, string title, int sort,
        List<FixedAsset> assets, int year, int prior, Func<FixedAsset, string> label)
    {
        if (assets.Count == 0) return null;

        // ถือครอง ณ สิ้นปี y = ได้มา ≤ y และยังไม่จำหน่าย (หรือจำหน่ายหลังปี y)
        static bool HeldAtEnd(FixedAsset a, int y)
            => a.AcquireDate.Year <= y && (a.DisposalDate is null || a.DisposalDate.Value.Year > y);

        var costAgg   = new Dictionary<string, (decimal Open, decimal Add, decimal Disp, decimal Close)>();
        var accumAgg  = new Dictionary<string, (decimal Open, decimal Add, decimal Disp, decimal Close)>();
        decimal chargeTotal = 0m;
        decimal chargePriorTotal = 0m;

        foreach (var a in assets)
        {
            var key = label(a);

            // ── ราคาทุน ──
            decimal openCost  = HeldAtEnd(a, prior) ? a.Cost : 0m;
            decimal closeCost = HeldAtEnd(a, year)  ? a.Cost : 0m;
            decimal addCost   = a.AcquireDate.Year == year ? a.Cost : 0m;
            decimal dispCost  = a.DisposalDate is { } dd && dd.Year == year ? a.Cost : 0m;

            var c = costAgg.GetValueOrDefault(key);
            costAgg[key] = (c.Open + openCost, c.Add + addCost, c.Disp + dispCost, c.Close + closeCost);

            // ── ค่าเสื่อมราคาสะสม ──
            // ใช้ยอดต้นปี/ปลายปีจาก AsOf ของ "ปีปัจจุบัน" (Opening = สะสมต้นปี รวมยอดยกมา;
            // Closing = Opening + Charge เสมอ) เพื่อให้ตาราง ต้น+เพิ่ม−ลด = ปลาย ลงตรงพอดี.
            // (ไม่ใช้ AsOf(ปีก่อน).Closing เพราะสินทรัพย์ที่ยกยอดมาปีปัจจุบันจะคืน 0 ทำให้ยอดต้นปีต่ำผิด)
            var bookCur   = DepreciationEngine.AsOf(a, a.BookRatePct, year);

            decimal openAccum  = HeldAtEnd(a, prior) ? bookCur.OpeningAccumulated : 0m;
            decimal charge     = bookCur.Charge;
            decimal closeAccum = HeldAtEnd(a, year)  ? bookCur.ClosingAccumulated : 0m;
            // ค่าเสื่อมสะสมที่ตัดออกตอนจำหน่าย = ยอดต้น + ค่าเสื่อมปีนั้น (ทำให้ปลายเป็น 0)
            decimal dispAccum  = a.DisposalDate is { } d2 && d2.Year == year ? openAccum + charge : 0m;

            var ac = accumAgg.GetValueOrDefault(key);
            accumAgg[key] = (ac.Open + openAccum, ac.Add + charge, ac.Disp + dispAccum, ac.Close + closeAccum);

            chargeTotal += charge;
            chargePriorTotal += DepreciationEngine.AsOf(a, a.BookRatePct, prior).Charge;
        }

        var costRows = costAgg
            .Where(kv => kv.Value.Open != 0 || kv.Value.Add != 0 || kv.Value.Disp != 0 || kv.Value.Close != 0)
            .OrderBy(kv => kv.Key)
            .Select(kv => Row(kv.Key, kv.Value))
            .ToList();
        var accumRows = accumAgg
            .Where(kv => kv.Value.Open != 0 || kv.Value.Add != 0 || kv.Value.Disp != 0 || kv.Value.Close != 0)
            .OrderBy(kv => kv.Key)
            .Select(kv => Row(kv.Key, kv.Value))
            .ToList();

        var costTotal  = Total("รวมราคาทุน", costRows);
        var accumTotal = Total("รวมค่าเสื่อมราคาสะสม", accumRows);

        return new NoteMovementDto(
            no, title, sort, costRows, costTotal, accumRows, accumTotal,
            Math.Round(costTotal.Opening - accumTotal.Opening, 2),
            Math.Round(costTotal.Closing - accumTotal.Closing, 2),
            Math.Round(chargeTotal, 2),
            Math.Round(chargePriorTotal, 2));

        static NoteMovementRowDto Row(string label, (decimal Open, decimal Add, decimal Disp, decimal Close) v)
            => new(label, Math.Round(v.Open, 2), Math.Round(v.Add, 2), Math.Round(v.Disp, 2), Math.Round(v.Close, 2));

        static NoteMovementRowDto Total(string label, List<NoteMovementRowDto> rows)
            => new(label,
                Math.Round(rows.Sum(r => r.Opening), 2),
                Math.Round(rows.Sum(r => r.Additions), 2),
                Math.Round(rows.Sum(r => r.Disposals), 2),
                Math.Round(rows.Sum(r => r.Closing), 2));
    }

    // ── Narrative (template ข้อความ) ─────────────────────────────────────────────

    private async Task<List<NoteNarrativeDto>> BuildNarrativesAsync(
        int clientCompanyId, int year, int prior, ClientCompany client, CancellationToken ct)
    {
        // ดึง template ที่ EffectiveYear ≤ ปีงบ ทั้ง default (null) และเฉพาะบริษัท
        var candidates = await db.NoteTemplateSections.AsNoTracking()
            .Where(s => (s.ClientCompanyId == null || s.ClientCompanyId == clientCompanyId)
                     && s.EffectiveYear <= year)
            .ToListAsync(ct);

        // ต่อ NoteKey เลือกตัวที่ดีที่สุด: บริษัท override ก่อน default; ภายในกลุ่ม EffectiveYear มากสุด
        var chosen = candidates
            .GroupBy(s => s.NoteKey)
            .Select(g => g
                .OrderByDescending(s => s.ClientCompanyId.HasValue) // company override > default
                .ThenByDescending(s => s.EffectiveYear)
                .First())
            .ToList();

        var ph = new NotesPlaceholders(
            CompanyName: client.LegalName,
            TaxId: string.IsNullOrWhiteSpace(client.TaxId) ? "-" : client.TaxId,
            Address: string.IsNullOrWhiteSpace(client.Address) ? "-" : client.Address!,
            FiscalYear: year, FiscalYearTh: year + 543,
            PriorYear: prior, PriorYearTh: prior + 543);

        return chosen
            .OrderBy(s => s.SortOrder)
            .Select(s => new NoteNarrativeDto(
                s.NoteKey, s.Title, NotesEngine.Substitute(s.BodyText, ph),
                s.SortOrder, s.EffectiveYear, s.ClientCompanyId.HasValue))
            .ToList();
    }
}
