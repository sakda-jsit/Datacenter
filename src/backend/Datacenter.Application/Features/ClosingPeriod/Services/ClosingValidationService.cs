using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Datacenter.Application.Features.FinancialStatement.Services;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Services;

/// <summary>
/// รวมตรรกะตรวจสอบความพร้อมก่อนปิดงวดบัญชีไว้ที่เดียว ใช้ทั้งใน query (preview)
/// และ command (บังคับตรวจก่อนปิดจริง) ตาม business rule:
/// "Closing validation must check VAT, AR/AP, bank reconciliation, and GL balance."
///
/// ข้อจำกัดของข้อมูลที่กำหนดวิธีตรวจ:
/// 1. GL ที่นำเข้าจาก Express เป็น <b>ยอดรวมรายปี</b> (OPEN-Y ลงวันสิ้นปีก่อน + MOVE-Y ลงวันสิ้นปี)
///    ไม่มีรายละเอียดรายเดือน → กระทบยอดรายเดือนทำไม่ได้ ตัวตรวจจึงทำงาน <b>เฉพาะงวดสิ้นปีบัญชี</b>
/// 2. เทียบด้วย <b>ยอดเคลื่อนไหวของปี (flow)</b> ไม่ใช่ยอดคงเหลือ เพราะ
///    (ก) บัญชีภาษีซื้อ/ขายถูกล้างทุกเดือนตอนนำส่ง ยอดคงเหลือปลายปีจึงเป็น 0 ทั้งที่มีภาษีทั้งปี
///    (ข) ยอดค้างลูกหนี้/เจ้าหนี้จาก Express เป็น snapshot ณ ตอนนำเข้า ไม่ใช่ยอด ณ สิ้นปีที่ปิด
///    (ค) ธนาคารตรวจคนละแบบ — ดู CheckBankAsync
/// 3. ผลกระทบยอดเป็น Warning ไม่บล็อกการปิดงวด — ผลต่างมักมีเหตุผลอธิบายได้ (timing/รายการปรับปรุง)
///    สิ่งที่บล็อกจริงมีอย่างเดียวคือ GL ไม่ดุล
/// </summary>
public static class ClosingValidationService
{
    private const decimal BalanceTolerance = 0.01m;

    /// <summary>ผลต่างที่ยอมรับได้ตอนกระทบยอดโมดูลกับ GL (บาท) — เผื่อการปัดเศษสะสมข้ามหลายร้อยรายการ</summary>
    private const decimal ReconTolerance = 1.00m;

    public static async Task<List<ClosingValidationItemDto>> ValidateAsync(
        IApplicationDbContext db, int clientCompanyId, int year, int month, CancellationToken ct)
    {
        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1);

        var monthLines = await db.JournalEntryLines
            .AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId
                     && l.JournalEntry.JournalDate >= periodStart
                     && l.JournalEntry.JournalDate < periodEnd)
            .Select(l => new { l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        var items = new List<ClosingValidationItemDto>();

        // 1) มีข้อมูล GL ในงวดหรือไม่
        bool hasData = monthLines.Count > 0;
        items.Add(new ClosingValidationItemDto(
            Code: "GL_HAS_DATA",
            Label: "มีรายการบัญชีในงวด",
            Severity: "Warning",
            Passed: hasData,
            Detail: hasData ? $"พบ {monthLines.Count} บรรทัดรายการ" : "ยังไม่มีรายการบัญชีในงวดนี้"));

        // 2) GL balanced (เดบิตรวม = เครดิตรวม)
        decimal totalDebit = monthLines.Sum(l => l.DebitAmount);
        decimal totalCredit = monthLines.Sum(l => l.CreditAmount);
        decimal diff = totalDebit - totalCredit;
        bool balanced = Math.Abs(diff) <= BalanceTolerance;
        items.Add(new ClosingValidationItemDto(
            Code: "GL_BALANCED",
            Label: "เดบิตรวมเท่ากับเครดิตรวม (GL balanced)",
            Severity: "Error",
            Passed: balanced,
            Detail: balanced
                ? $"เดบิต {totalDebit:N2} = เครดิต {totalCredit:N2}"
                : $"ผลต่าง {diff:N2} (เดบิต {totalDebit:N2} / เครดิต {totalCredit:N2})"));

        // 3-5) กระทบยอดโมดูลกับ GL — ทำได้จริงเฉพาะงวดสิ้นปีบัญชี (ดูหมายเหตุหัวคลาส)
        int startMonth = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.Id == clientCompanyId)
            .Select(c => c.FiscalYearStartMonth)
            .FirstOrDefaultAsync(ct);
        if (startMonth is < 1 or > 12) startMonth = 1;

        int yearEndMonth = startMonth == 1 ? 12 : startMonth - 1;
        int fiscalYear = month >= startMonth ? year : year - 1;
        var fiscalYearStart = new DateTime(fiscalYear, startMonth, 1);
        var fiscalYearEnd = fiscalYearStart.AddYears(1).AddDays(-1);

        if (month != yearEndMonth)
        {
            string why = $"ตรวจกระทบยอดที่งวดสิ้นปีบัญชี (เดือน {yearEndMonth}) — "
                       + "ข้อมูล GL จาก Express เป็นยอดรวมรายปี ไม่มีรายละเอียดรายเดือน";
            items.Add(new ClosingValidationItemDto("VAT_RECONCILED", "กระทบยอดภาษีมูลค่าเพิ่มกับ GL", "Info", true, why));
            items.Add(new ClosingValidationItemDto("ARAP_RECONCILED", "กระทบยอดลูกหนี้/เจ้าหนี้กับ GL", "Info", true, why));
            items.Add(new ClosingValidationItemDto("BANK_RECONCILED", "กระทบยอดธนาคาร (statement)", "Info", true, why));
            return items;
        }

        // เทียบ "การเคลื่อนไหวของปี" → ใช้เฉพาะ MOVE-Y (ตัดยอดยกมา OPEN-Y/CF-Y ออก)
        var fyEntryIds = await FsJournalNets.FiscalYearEntryIdsAsync(db, clientCompanyId, fiscalYear, ct);
        var openingIds = await FsJournalNets.OpeningEntryIdsAsync(db, clientCompanyId, fiscalYear, ct);
        var moveIds = fyEntryIds.Except(openingIds).ToList();

        items.Add(await CheckVatAsync(db, clientCompanyId, fiscalYear, moveIds, ct));
        items.Add(await CheckArApAsync(db, clientCompanyId, fiscalYearStart, fiscalYearEnd, moveIds, ct));
        items.Add(await CheckBankAsync(db, clientCompanyId, fiscalYearStart, fiscalYearEnd, ct));

        return items;
    }

    /// <summary>ปิดงวดได้เมื่อไม่มี item ที่เป็น Error และยังไม่ผ่าน</summary>
    public static bool CanClose(IEnumerable<ClosingValidationItemDto> items)
        => items.All(i => i.Severity != "Error" || i.Passed);

    // ───────────────────────── ตัวตรวจรายโมดูล ─────────────────────────

    /// <summary>
    /// ภาษีขาย/ภาษีซื้อของปีจาก ISVAT เทียบ <b>ยอดเคลื่อนไหว</b>ในบัญชีภาษีของ GL
    /// (ภาษีขาย = ยอดเครดิต · ภาษีซื้อ = ยอดเดบิต).
    /// หาบัญชีจากชื่อเพราะผังบัญชีไม่มีฟิลด์บอกว่าบัญชีไหนคือบัญชีภาษี — และตัด
    /// บัญชีพัก/รอ (Suspense) ออก เพราะเป็นภาษีที่ยังไม่เข้า ภ.พ.30 ของงวดนี้
    /// </summary>
    private static async Task<ClosingValidationItemDto> CheckVatAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, List<int> moveIds, CancellationToken ct)
    {
        const string code = "VAT_RECONCILED";
        const string label = "กระทบยอดภาษีมูลค่าเพิ่มกับ GL";

        var vat = await db.VatEntries.AsNoTracking()
            .Where(v => v.ClientCompanyId == clientCompanyId && v.TaxPeriod.Year == fiscalYear)
            .Select(v => new { v.VatType, v.VatAmount })
            .ToListAsync(ct);

        if (vat.Count == 0)
            return Skipped(code, label, $"ยังไม่มีข้อมูลภาษีมูลค่าเพิ่มของปี {fiscalYear} — ข้ามการตรวจ");

        decimal outputVat = vat.Where(v => v.VatType == VatEntryType.Output).Sum(v => v.VatAmount);
        decimal inputVat = vat.Where(v => v.VatType == VatEntryType.Input).Sum(v => v.VatAmount);

        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == clientCompanyId)
            .Select(a => new { a.Id, a.AccountCode, a.AccountName, a.AccountType })
            .ToListAsync(ct);

        // ภาษีขายต้องเป็นหนี้สิน · ภาษีซื้อต้องเป็นสินทรัพย์ — กันบัญชีค่าใช้จ่ายอย่าง
        // "ภาษีซื้อไม่ขอคืน" (VAT non-refundable) ที่ไม่ได้อยู่ใน ภ.พ.30 หลุดเข้ามา
        var outAcc = accounts
            .Where(a => a.AccountType == AccountType.Liability
                     && NameHas(a.AccountName, "ภาษีขาย", "Sales Tax", "Output Tax", "Output VAT")
                     && !IsSuspense(a.AccountName))
            .ToList();
        var inAcc = accounts
            .Where(a => a.AccountType == AccountType.Asset
                     && NameHas(a.AccountName, "ภาษีซื้อ", "Purchase Tax", "Input Tax", "Input VAT")
                     && !IsSuspense(a.AccountName))
            .ToList();

        if (outAcc.Count == 0 && inAcc.Count == 0)
            return Skipped(code, label, "ไม่พบบัญชีภาษีซื้อ/ภาษีขายในผังบัญชี — ตรวจอัตโนมัติไม่ได้ ให้กระทบยอดเอง");

        var mv = await MovementByAccountAsync(db, moveIds, ct);
        decimal glOutput = outAcc.Sum(a => mv.GetValueOrDefault(a.Id).Credit);
        decimal glInput = inAcc.Sum(a => mv.GetValueOrDefault(a.Id).Debit);

        decimal dOut = Math.Round(outputVat - glOutput, 2);
        decimal dIn = Math.Round(inputVat - glInput, 2);
        bool passed = Math.Abs(dOut) <= ReconTolerance && Math.Abs(dIn) <= ReconTolerance;

        string detail =
            $"ภาษีขาย ISVAT {outputVat:N2} / GL เครดิต {glOutput:N2} (ต่าง {dOut:N2}) · " +
            $"ภาษีซื้อ ISVAT {inputVat:N2} / GL เดบิต {glInput:N2} (ต่าง {dIn:N2}) · " +
            $"บัญชีที่ใช้เทียบ: {AccountList(outAcc.Concat(inAcc).Select(a => a.AccountCode))}";

        return new ClosingValidationItemDto(code, label, "Warning", passed, detail);
    }

    /// <summary>
    /// มูลค่าใบแจ้งหนี้/ใบตั้งหนี้ที่ออกในปี เทียบยอดเคลื่อนไหวบัญชีลูกหนี้/เจ้าหนี้ใน GL
    /// (ลูกหนี้ = ยอดเดบิต · เจ้าหนี้ = ยอดเครดิต). บัญชี GL มาจาก ACCNUM ในทะเบียนลูกค้า/ผู้ขาย
    /// จึงไม่ต้องเดาจากชื่อ. ใช้ "ยอดที่ออกในปี" แทน "ยอดคงค้าง" เพราะยอดคงค้างที่ Express ให้มา
    /// เป็น snapshot ณ ตอนนำเข้า ไม่ใช่ยอด ณ สิ้นปีที่กำลังปิด
    /// </summary>
    private static async Task<ClosingValidationItemDto> CheckArApAsync(
        IApplicationDbContext db, int clientCompanyId, DateTime fyStart, DateTime fyEnd,
        List<int> moveIds, CancellationToken ct)
    {
        const string code = "ARAP_RECONCILED";
        const string label = "กระทบยอดลูกหนี้/เจ้าหนี้กับ GL";

        decimal arIssued = await db.ArInvoices.AsNoTracking()
            .Where(i => i.ClientCompanyId == clientCompanyId
                     && i.DocumentDate >= fyStart && i.DocumentDate <= fyEnd)
            .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0m;
        decimal apIssued = await db.ApInvoices.AsNoTracking()
            .Where(i => i.ClientCompanyId == clientCompanyId
                     && i.DocumentDate >= fyStart && i.DocumentDate <= fyEnd)
            .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0m;

        if (arIssued == 0m && apIssued == 0m)
            return Skipped(code, label, "ไม่มีใบแจ้งหนี้/ใบตั้งหนี้ในปีบัญชีนี้ — ข้ามการตรวจ");

        var arCodes = await db.Customers.AsNoTracking()
            .Where(c => c.ClientCompanyId == clientCompanyId && c.GlAccountCode != null && c.GlAccountCode != "")
            .Select(c => c.GlAccountCode!).Distinct().ToListAsync(ct);
        var apCodes = await db.Suppliers.AsNoTracking()
            .Where(s => s.ClientCompanyId == clientCompanyId && s.GlAccountCode != null && s.GlAccountCode != "")
            .Select(s => s.GlAccountCode!).Distinct().ToListAsync(ct);

        if (arCodes.Count == 0 && apCodes.Count == 0)
            return Skipped(code, label, "ทะเบียนลูกค้า/ผู้ขายยังไม่ระบุบัญชี GL — ตรวจอัตโนมัติไม่ได้ ให้กระทบยอดเอง");

        var codeToId = await AccountCodeMapAsync(db, clientCompanyId, ct);
        var mv = await MovementByAccountAsync(db, moveIds, ct);

        decimal glAr = arCodes.Select(c => c.Trim()).Where(codeToId.ContainsKey)
            .Sum(c => mv.GetValueOrDefault(codeToId[c]).Debit);
        decimal glAp = apCodes.Select(c => c.Trim()).Where(codeToId.ContainsKey)
            .Sum(c => mv.GetValueOrDefault(codeToId[c]).Credit);

        decimal dAr = Math.Round(arIssued - glAr, 2);
        decimal dAp = Math.Round(apIssued - glAp, 2);
        bool passed = Math.Abs(dAr) <= ReconTolerance && Math.Abs(dAp) <= ReconTolerance;

        string detail =
            $"ใบแจ้งหนี้ที่ออกในปี {arIssued:N2} / GL เดบิตลูกหนี้ {glAr:N2} (ต่าง {dAr:N2}) · " +
            $"ใบตั้งหนี้ที่ออกในปี {apIssued:N2} / GL เครดิตเจ้าหนี้ {glAp:N2} (ต่าง {dAp:N2})";

        return new ClosingValidationItemDto(code, label, "Warning", passed, detail);
    }

    /// <summary>
    /// ตรวจว่างานกระทบยอดธนาคารทำครบแล้วหรือยัง — ทุกบัญชีมี statement ของปีนั้น
    /// อ่านไฟล์ได้ครบ (ตรวจยอดผ่าน) และจับคู่รายการครบทุกบรรทัด.
    ///
    /// หมายเหตุ: <b>ไม่</b> เทียบสมุดเงินฝาก (BKTRN) กับบัญชีเงินฝากใน GL ตรง ๆ เพราะ BKTRN ของ Express
    /// เก็บเฉพาะรายการบางประเภท ขณะที่บัญชี GL รับรายการจากทุกโมดูล (รับชำระ/จ่ายชำระ/ใบสำคัญ)
    /// สองยอดจึงไม่เท่ากันโดยธรรมชาติ — เทียบแล้วจะเตือนผิดทุกบริษัท ไม่ช่วยอะไร
    /// </summary>
    private static async Task<ClosingValidationItemDto> CheckBankAsync(
        IApplicationDbContext db, int clientCompanyId, DateTime fyStart, DateTime fyEnd,
        CancellationToken ct)
    {
        const string code = "BANK_RECONCILED";
        const string label = "กระทบยอดธนาคาร (statement)";

        var accounts = await db.BankAccounts.AsNoTracking()
            .Where(b => b.ClientCompanyId == clientCompanyId && b.IsActive)
            .Select(b => new { b.Id, b.BankName, b.AccountNumber, b.BankAccountCode })
            .ToListAsync(ct);

        if (accounts.Count == 0)
            return Skipped(code, label, "ยังไม่มีข้อมูลบัญชีธนาคาร — ข้ามการตรวจ");

        var imports = await db.BankStatementImports.AsNoTracking()
            .Where(i => i.ClientCompanyId == clientCompanyId
                     && i.PeriodEnd >= fyStart && i.PeriodStart <= fyEnd)
            .Select(i => new
            {
                i.Id,
                i.BankAccountId,
                i.ParsedOk,
                LineCount = i.Lines.Count,
                MatchedCount = i.Lines.Count(l => l.MatchStatus != BankLineMatchStatus.Unmatched),
            })
            .ToListAsync(ct);

        var problems = new List<string>();
        int done = 0;
        foreach (var a in accounts)
        {
            string name = $"{a.BankName} {a.AccountNumber ?? a.BankAccountCode}";
            var mine = imports.Where(i => i.BankAccountId == a.Id).ToList();
            if (mine.Count == 0)
            {
                problems.Add($"{name}: ยังไม่ได้นำเข้า statement");
                continue;
            }

            int lines = mine.Sum(i => i.LineCount);
            int matched = mine.Sum(i => i.MatchedCount);
            bool parsedOk = mine.All(i => i.ParsedOk);

            if (!parsedOk)
                problems.Add($"{name}: ตรวจยอด statement ไม่ผ่าน");
            else if (lines > 0 && matched < lines)
                problems.Add($"{name}: จับคู่ {matched}/{lines} รายการ");
            else
                done++;
        }

        bool passed = problems.Count == 0;
        string detail = passed
            ? $"กระทบยอดครบทุกบัญชี ({done}/{accounts.Count})"
            : $"ยังไม่เรียบร้อย {problems.Count}/{accounts.Count} บัญชี — {string.Join(" · ", problems.Take(3))}"
              + (problems.Count > 3 ? $" (และอีก {problems.Count - 3})" : "");

        return new ClosingValidationItemDto(code, label, "Warning", passed, detail);
    }

    // ───────────────────────── helper ─────────────────────────

    /// <summary>ยอดเดบิต/เครดิตรวมต่อบัญชี ของ JournalEntry ชุดที่ระบุ</summary>
    private static async Task<Dictionary<int, (decimal Debit, decimal Credit)>> MovementByAccountAsync(
        IApplicationDbContext db, List<int> entryIds, CancellationToken ct)
    {
        if (entryIds.Count == 0) return [];
        var rows = await db.JournalEntryLines.AsNoTracking()
            .Where(l => entryIds.Contains(l.JournalEntryId))
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.AccountId, r => (r.Debit, r.Credit));
    }

    /// <summary>รหัสบัญชี → id (รหัสซ้ำเลือกตัวแรก)</summary>
    private static async Task<Dictionary<string, int>> AccountCodeMapAsync(
        IApplicationDbContext db, int clientCompanyId, CancellationToken ct)
    {
        var rows = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == clientCompanyId)
            .Select(a => new { a.Id, a.AccountCode })
            .ToListAsync(ct);
        return rows.GroupBy(a => a.AccountCode).ToDictionary(g => g.Key, g => g.First().Id);
    }

    private static bool NameHas(string name, params string[] keywords)
        => keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));

    /// <summary>บัญชีพักภาษี (ยังไม่เข้า ภ.พ.30 ของงวดนี้)</summary>
    private static bool IsSuspense(string name)
        => NameHas(name, "Suspense", "พัก", "รอเรียก", "ยังไม่ถึงกำหนด");

    private static string AccountList(IEnumerable<string> codes)
    {
        var list = codes.Distinct().OrderBy(c => c).ToList();
        return list.Count == 0 ? "—"
            : string.Join(", ", list.Take(6)) + (list.Count > 6 ? $" (และอีก {list.Count - 6})" : "");
    }

    private static ClosingValidationItemDto Skipped(string code, string label, string detail)
        => new(code, label, "Info", true, detail);
}
