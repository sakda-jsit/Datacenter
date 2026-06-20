using Datacenter.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Services;

/// <summary>
/// คำนวณยอดบัญชี (net = Debit − Credit) ต่อ AccountCode ตามปีงบ จากรูปแบบการลงบัญชีของ
/// Express importer (<c>ExpressPostingService</c>): ต่อปีงบ Y มี 2 entry —
/// <c>OPEN-Y</c> (SourceModule="OpeningBalance", FiscalYear=Y = ยอดยกมา snapshot เต็ม) และ
/// <c>MOVE-Y</c> (SourceModule="ImportBalance", FiscalYear=Y = ยอดเคลื่อนไหวสะสมปีนั้น),
/// โดย <b>ยอดปิดปี Y = OPEN-Y + MOVE-Y</b>.
///
/// กรองด้วย <c>FiscalYear == Y</c> (explicit) แล้วใช้ <c>SourceModule</c> แยก opening/movement —
/// แทนการเดาปีจาก JournalDate ซึ่งเปราะ เพราะ OPEN-(Y+1) ลงวันที่ Y-12-31 ชนกับ MOVE-Y
/// (เคยทำให้งบเบิ้ล ≈2 เท่าเมื่อบริษัทมีข้อมูล ≥2 ปี — ดู fs-cumulative-double-count).
/// FiscalYear ตัดปัญหานี้เด็ดขาดทุกจำนวนปี.
/// </summary>
public static class FsJournalNets
{
    private const string Opening = "OpeningBalance";

    /// <summary>ยอดยกมาต้นปี Y (OPEN-Y) ต่อ AccountCode.</summary>
    public static Task<Dictionary<string, decimal>> OpeningAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct) =>
        NetsAsync(db, l =>
            l.JournalEntry.ClientCompanyId == clientCompanyId &&
            l.JournalEntry.FiscalYear == fiscalYear &&
            l.JournalEntry.SourceModule == Opening, ct);

    /// <summary>ยอดเคลื่อนไหวระหว่างปี Y (MOVE-Y) ต่อ AccountCode.</summary>
    public static Task<Dictionary<string, decimal>> MovementAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct) =>
        NetsAsync(db, l =>
            l.JournalEntry.ClientCompanyId == clientCompanyId &&
            l.JournalEntry.FiscalYear == fiscalYear &&
            l.JournalEntry.SourceModule != Opening, ct);

    /// <summary>ยอดสะสมถึงสิ้นปี Y = OPEN-Y + MOVE-Y ต่อ AccountCode.</summary>
    public static Task<Dictionary<string, decimal>> CumulativeAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct) =>
        NetsAsync(db, l =>
            l.JournalEntry.ClientCompanyId == clientCompanyId &&
            l.JournalEntry.FiscalYear == fiscalYear, ct);

    /// <summary>
    /// รหัส JournalEntry ของ {OPEN-Y, MOVE-Y} (ยอดยกมา + เคลื่อนไหว) สำหรับปีงบ Y —
    /// ใช้ใน query กระดาษทำการที่กรองตาม account/Dr-Cr: filter ด้วย
    /// <c>ids.Contains(l.JournalEntryId)</c> แทนการตัดด้วยวันที่ (กัน OPEN-(Y+1) เบิ้ล).
    /// </summary>
    public static Task<List<int>> FiscalYearEntryIdsAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct) =>
        db.JournalEntries.AsNoTracking()
            .Where(j => j.ClientCompanyId == clientCompanyId && j.FiscalYear == fiscalYear)
            .Select(j => j.Id)
            .ToListAsync(ct);

    /// <summary>รหัส JournalEntry ของ OPEN-Y (ยอดยกมาต้นปี) เท่านั้น — สำหรับคอลัมน์ "ยอดต้นปี".</summary>
    public static Task<List<int>> OpeningEntryIdsAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct) =>
        db.JournalEntries.AsNoTracking()
            .Where(j => j.ClientCompanyId == clientCompanyId
                     && j.FiscalYear == fiscalYear
                     && j.SourceModule == Opening)
            .Select(j => j.Id)
            .ToListAsync(ct);

    private static async Task<Dictionary<string, decimal>> NetsAsync(
        IApplicationDbContext db,
        System.Linq.Expressions.Expression<Func<Datacenter.Domain.Entities.JournalEntryLine, bool>> predicate,
        CancellationToken ct)
    {
        var lines = await db.JournalEntryLines.AsNoTracking()
            .Where(predicate)
            .Select(l => new { l.Account.AccountCode, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);

        return lines
            .GroupBy(l => l.AccountCode)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.DebitAmount - l.CreditAmount));
    }
}
