using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FinancialStatement.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ReportPackages.Services;

/// <summary>
/// Option B — carry รายการปรับปรุงปิดงบ (AJE) ของปีที่ finalize แล้ว เข้าเป็น "ยอดยกมา" ของปีถัดไป.
/// สร้าง JournalEntry พิเศษ <c>CF-{Y}</c> (SourceModule=<see cref="FsJournalNets.CarryForwardOpening"/>,
/// FiscalYear=Y, ลงวันที่ (Y-1)-12-31) = ผลรวม AJE ของปี finalized ทุกปี &lt; Y
/// โดย <b>map บัญชี P&amp;L → บัญชีกำไรสะสม (RE)</b> (กำไร/ขาดทุนปิดเข้า RE ทุกปี) ส่วนบัญชีงบดุลยกตรง.
///
/// AJE เป็นรายการใน-ระบบเท่านั้น (ไม่อยู่ใน Express) → Express brought-forward (OPEN-Y) ไม่มี →
/// CF-Y จึงเป็น delta ที่เติมให้ opening ตรงกับ "ยอดปลายปีก่อนหลังปรับปรุง" (ไม่เบิ้ลกับ OPEN-Y).
/// </summary>
public class CarryForwardService(IApplicationDbContext db, ICurrentUserService currentUser)
{
    /// <summary>
    /// หลังเปลี่ยนสถานะ Report Package ของปี <paramref name="changedYear"/> (ข้ามเส้น finalized) →
    /// regenerate CF ของทุกปีถัดไปที่มีข้อมูล GL และไม่ถูกล็อก. คืน warnings (RE หลายตัว / ปี locked).
    /// </summary>
    public async Task<List<string>> RegenerateAfterStatusChangeAsync(
        int companyId, int changedYear, bool changedYearFinalizedAfter, CancellationToken ct)
    {
        var warnings = new List<string>();

        var subsequentYears = await db.JournalEntries.AsNoTracking()
            .Where(j => j.ClientCompanyId == companyId && j.FiscalYear > changedYear)
            .Select(j => j.FiscalYear)
            .Distinct()
            .ToListAsync(ct);

        foreach (var y in subsequentYears.OrderBy(x => x))
            warnings.AddRange(await RegenerateAsync(companyId, y, changedYear, changedYearFinalizedAfter, ct));

        return warnings;
    }

    /// <summary>
    /// (re)สร้าง CF-{intoYear} = ผลรวม AJE ของปี finalized &lt; intoYear (map P&amp;L→RE).
    /// idempotent: ลบ CF เดิมก่อนเสมอ. ไม่เรียก SaveChanges (ให้ caller commit รวมกับ status).
    /// </summary>
    /// <param name="changedYear">ปีที่เพิ่งเปลี่ยนสถานะ (สถานะใน DB อาจยังไม่ commit จึงรับมาเป็น param)</param>
    /// <param name="changedYearFinalizedAfter">หลังเปลี่ยน ปีนั้นอยู่ในชุด finalized (Final/Locked) หรือไม่</param>
    private async Task<List<string>> RegenerateAsync(
        int companyId, int intoYear, int changedYear, bool changedYearFinalizedAfter, CancellationToken ct)
    {
        var warnings = new List<string>();
        var cfDoc = $"CF-{intoYear}";

        // ปีปลายทางถูกล็อก (ยื่นแล้ว) → ห้ามแตะ opening (immutability) — เตือน stale ถ้ามี CF เดิม
        bool intoYearLocked = await db.ReportPackages.AsNoTracking().AnyAsync(
            p => p.ClientCompanyId == companyId && p.FiscalYear == intoYear
              && p.Status == ReportPackageStatus.Locked, ct);
        if (intoYearLocked)
        {
            warnings.Add($"ปีงบ {intoYear} ถูกล็อก — ไม่ปรับยอดยกมา (opening อาจไม่ตรงกับปีก่อนที่เปลี่ยนแปลง)");
            return warnings;
        }

        // ลบ CF เดิมของปีนี้เสมอ (idempotent)
        var existing = await db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.ClientCompanyId == companyId && j.DocumentNo == cfDoc)
            .ToListAsync(ct);
        if (existing.Count > 0) db.JournalEntries.RemoveRange(existing);

        // ปีที่ finalized (Final/Locked) < intoYear — ปรับสถานะของ changedYear ที่อาจยังไม่ commit
        var finalizedYears = (await db.ReportPackages.AsNoTracking()
            .Where(p => p.ClientCompanyId == companyId && p.FiscalYear < intoYear
                     && (p.Status == ReportPackageStatus.Final || p.Status == ReportPackageStatus.Locked))
            .Select(p => p.FiscalYear)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();
        if (changedYear < intoYear)
        {
            if (changedYearFinalizedAfter) finalizedYears.Add(changedYear);
            else finalizedYears.Remove(changedYear);
        }
        if (finalizedYears.Count == 0) return warnings; // ไม่มีปี finalized ก่อนหน้า → CF ว่าง (ลบแล้ว)

        // ผลรวม AJE ของปี finalized < intoYear
        var adjLines = await db.AdjustmentEntryLines.AsNoTracking()
            .Where(l => l.AdjustmentEntry.ClientCompanyId == companyId
                     && finalizedYears.Contains(l.AdjustmentEntry.FiscalYear))
            .Select(l => new { l.AccountId, l.Account.AccountType, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);
        if (adjLines.Count == 0) return warnings; // ไม่มี AJE → CF ว่าง (ลบแล้ว)

        // บัญชีกำไรสะสม (RE) — ปลายทางของ P&L ที่ปิดเข้าทุกปี
        bool needsRe = adjLines.Any(l => l.AccountType is AccountType.Income or AccountType.Expense);
        int? reAccountId = null;
        if (needsRe)
        {
            var reCodes = await db.AccountStatementMappings.AsNoTracking()
                .Where(m => m.ClientCompanyId == companyId && m.RefCode == "RE")
                .Select(m => m.AccountCode)
                .ToListAsync(ct);
            var reAcc = await db.Accounts.AsNoTracking()
                .Where(a => a.ClientCompanyId == companyId && reCodes.Contains(a.AccountCode))
                .OrderBy(a => a.AccountCode)
                .FirstOrDefaultAsync(ct);
            if (reAcc is null)
                throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                    "RetainedEarnings",
                    "ไม่พบบัญชีกำไรสะสม (RefCode=RE) ของบริษัท — ตั้ง mapping บัญชี→RE ก่อนปิดงบ เพื่อยกผลกระทบ AJE ของบัญชีกำไร/ขาดทุนไปปีถัดไป") });
            reAccountId = reAcc.Id;
            if (reCodes.Count > 1)
                warnings.Add($"บริษัทมีบัญชี RE หลายตัว — ลงยอดยกมาจาก AJE ที่บัญชี {reAcc.AccountCode}");
        }

        // map P&L → RE, BS เก็บเดิม; รวม net ต่อบัญชี (remap ไม่แตะ Dr/Cr → ยอดรวมยังสมดุล)
        var netByAccount = new Dictionary<int, decimal>();
        foreach (var l in adjLines)
        {
            int acctId = l.AccountType is AccountType.Income or AccountType.Expense
                ? reAccountId!.Value
                : l.AccountId;
            netByAccount[acctId] = netByAccount.GetValueOrDefault(acctId) + (l.DebitAmount - l.CreditAmount);
        }

        var cf = new JournalEntry
        {
            ClientCompanyId = companyId,
            DocumentNo = cfDoc,
            FiscalYear = intoYear,
            JournalDate = new DateTime(intoYear - 1, 12, 31),
            Description = $"ยอดยกมาจากรายการปรับปรุงปิดงบปีก่อน (carry-forward) ปี {intoYear}",
            SourceModule = FsJournalNets.CarryForwardOpening,
            CreatedBy = currentUser.Username,
        };
        foreach (var (acctId, net) in netByAccount)
        {
            if (net == 0) continue;
            cf.Lines.Add(new JournalEntryLine
            {
                AccountId = acctId,
                DebitAmount = net > 0 ? net : 0,
                CreditAmount = net < 0 ? -net : 0,
                Description = "ยกมาจาก AJE ปีก่อน",
            });
        }
        if (cf.Lines.Count > 0) db.JournalEntries.Add(cf);

        return warnings;
    }
}
