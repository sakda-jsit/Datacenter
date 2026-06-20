using Datacenter.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Services;

/// <summary>
/// คำนวณยอดบัญชี (net = Debit − Credit) ต่อ AccountCode ตามปีงบ จากรูปแบบการลงบัญชีของ
/// Express importer (<c>ExpressPostingService</c>): ต่อปีงบ Y มี 2 entry —
/// <c>OPEN-Y</c> (SourceModule="OpeningBalance", ลงวันที่ (Y-1)-12-31 = ยอดยกมา snapshot เต็ม) และ
/// <c>MOVE-Y</c> (SourceModule="ImportBalance", ลงวันที่ Y-12-31 = ยอดเคลื่อนไหวสะสมปีนั้น),
/// โดย <b>ยอดปิดปี Y = OPEN-Y + MOVE-Y</b>.
///
/// ห้าม sum ทุก entry แบบ <c>JournalDate &lt; สิ้นปี</c> (cumulative-from-inception) เพราะจะดึง
/// opening snapshot ของปีถัดไป (<c>OPEN-(Y+1)</c>, ลงวันที่ Y-12-31 ซึ่ง = ยอดปิดปี Y ที่ restate
/// ใหม่) มารวมด้วย → งบเบิ้ล ≈2 เท่าเมื่อบริษัทมีข้อมูล ≥2 ปี. การกรองด้วย SourceModule + ช่วงปี
/// (opening จากปีก่อนหน้า, movement จากปีนั้น) จึงคัด OPEN-Y + MOVE-Y ได้แม่นยำทุกจำนวนปี.
/// </summary>
public static class FsJournalNets
{
    /// <summary>ยอดยกมาต้นปี Y (OPEN-Y) ต่อ AccountCode.</summary>
    public static Task<Dictionary<string, decimal>> OpeningAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct)
    {
        var openingFrom = new DateTime(fiscalYear - 1, 1, 1);
        var fyStart     = new DateTime(fiscalYear, 1, 1);
        return NetsAsync(db, l =>
            l.JournalEntry.ClientCompanyId == clientCompanyId &&
            l.JournalEntry.SourceModule == "OpeningBalance" &&
            l.JournalEntry.JournalDate >= openingFrom &&
            l.JournalEntry.JournalDate < fyStart, ct);
    }

    /// <summary>ยอดเคลื่อนไหวระหว่างปี Y (MOVE-Y) ต่อ AccountCode.</summary>
    public static Task<Dictionary<string, decimal>> MovementAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct)
    {
        var fyStart = new DateTime(fiscalYear, 1, 1);
        var fyEnd   = new DateTime(fiscalYear + 1, 1, 1);
        return NetsAsync(db, l =>
            l.JournalEntry.ClientCompanyId == clientCompanyId &&
            l.JournalEntry.SourceModule != "OpeningBalance" &&
            l.JournalEntry.JournalDate >= fyStart &&
            l.JournalEntry.JournalDate < fyEnd, ct);
    }

    /// <summary>ยอดสะสมถึงสิ้นปี Y = OPEN-Y + MOVE-Y ต่อ AccountCode.</summary>
    public static Task<Dictionary<string, decimal>> CumulativeAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct)
    {
        var openingFrom = new DateTime(fiscalYear - 1, 1, 1);
        var fyStart     = new DateTime(fiscalYear, 1, 1);
        var fyEnd       = new DateTime(fiscalYear + 1, 1, 1);
        return NetsAsync(db, l =>
            l.JournalEntry.ClientCompanyId == clientCompanyId &&
            ((l.JournalEntry.SourceModule == "OpeningBalance"
                 && l.JournalEntry.JournalDate >= openingFrom
                 && l.JournalEntry.JournalDate < fyStart)
             || (l.JournalEntry.SourceModule != "OpeningBalance"
                 && l.JournalEntry.JournalDate >= fyStart
                 && l.JournalEntry.JournalDate < fyEnd)), ct);
    }

    /// <summary>
    /// รหัส JournalEntry ของ {OPEN-Y, MOVE-Y} (ยอดยกมา + เคลื่อนไหว) สำหรับปีงบ Y —
    /// ใช้ใน query กระดาษทำการที่กรองตาม account/Dr-Cr: filter ด้วย
    /// <c>ids.Contains(l.JournalEntryId)</c> แทนการตัดด้วย <c>JournalDate &lt; สิ้นปี</c>
    /// เพื่อไม่ดึง OPEN-(Y+1) มาเบิ้ล (ดู fs-cumulative-double-count).
    /// </summary>
    public static Task<List<int>> FiscalYearEntryIdsAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct)
    {
        var openingFrom = new DateTime(fiscalYear - 1, 1, 1);
        var fyStart     = new DateTime(fiscalYear, 1, 1);
        var fyEnd       = new DateTime(fiscalYear + 1, 1, 1);
        return db.JournalEntries.AsNoTracking()
            .Where(j => j.ClientCompanyId == clientCompanyId &&
                ((j.SourceModule == "OpeningBalance"
                     && j.JournalDate >= openingFrom && j.JournalDate < fyStart)
                 || (j.SourceModule != "OpeningBalance"
                     && j.JournalDate >= fyStart && j.JournalDate < fyEnd)))
            .Select(j => j.Id)
            .ToListAsync(ct);
    }

    /// <summary>รหัส JournalEntry ของ OPEN-Y (ยอดยกมาต้นปี) เท่านั้น — สำหรับคอลัมน์ "ยอดต้นปี".</summary>
    public static Task<List<int>> OpeningEntryIdsAsync(
        IApplicationDbContext db, int clientCompanyId, int fiscalYear, CancellationToken ct)
    {
        var openingFrom = new DateTime(fiscalYear - 1, 1, 1);
        var fyStart     = new DateTime(fiscalYear, 1, 1);
        return db.JournalEntries.AsNoTracking()
            .Where(j => j.ClientCompanyId == clientCompanyId
                     && j.SourceModule == "OpeningBalance"
                     && j.JournalDate >= openingFrom && j.JournalDate < fyStart)
            .Select(j => j.Id)
            .ToListAsync(ct);
    }

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
