using Datacenter.Application.Features.Prepaid.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Prepaid.Services;

/// <summary>
/// ตัดจ่ายค่าใช้จ่ายจ่ายล่วงหน้าแบบเส้นตรงรายวัน (pure — ไม่แตะ DB, req v11 docs/14 PREPAID).
///
/// หลักการ (ตรง spec "กระจายยอดตามวันที่เริ่ม–สิ้นสุด, เดือน/ปีแรก prorate ตามวัน"):
/// - อัตราต่อวัน = มูลค่าตั้งต้น / จำนวนวันทั้งช่วง (เริ่ม–สิ้นสุด รวมปลาย)
/// - ตัดจ่ายปีงบ = round(อัตราต่อวัน × วันของช่วงที่ตกในปีนั้น, 2)
/// - ปีสุดท้าย absorb เศษ → ตัดจ่ายสะสม = มูลค่าตั้งต้นเป๊ะ (คงเหลือ 0)
/// - ตัดจ่ายสะสมไม่เกินมูลค่าตั้งต้น; คงเหลือไม่ติดลบ
/// </summary>
public static class PrepaidAmortizationEngine
{
    /// <summary>จำนวนวันทั้งช่วง (รวมปลายทั้งสองด้าน); &lt;=0 ถ้าวันไม่ถูกต้อง</summary>
    public static int TotalDays(PrepaidExpense p) => (p.EndDate.Date - p.StartDate.Date).Days + 1;

    /// <summary>สร้างตารางตัดจ่ายรายปี ตั้งแต่ปีเริ่มถึงปีสิ้นสุด</summary>
    public static IReadOnlyList<PrepaidYearDto> BuildSchedule(PrepaidExpense p)
    {
        var rows = new List<PrepaidYearDto>();
        var total = Math.Round(p.TotalAmount, 2);
        var totalDays = TotalDays(p);
        if (total <= 0m || totalDays <= 0) return rows;

        var amortized = 0m;
        for (var year = p.StartDate.Year; year <= p.EndDate.Year; year++)
        {
            var opening = amortized;
            var charge = ChargeForYear(p, year, total, totalDays);

            var remaining = Math.Round(total - amortized, 2);
            // ปีสุดท้าย (หรือ charge เกินคงเหลือ) → absorb เศษให้ปิดพอดี
            if (year == p.EndDate.Year || charge > remaining) charge = remaining;
            if (charge < 0m) charge = 0m;

            amortized = Math.Round(amortized + charge, 2);
            rows.Add(new PrepaidYearDto(year, opening, charge, amortized, Math.Round(total - amortized, 2)));
        }

        return rows;
    }

    private static decimal ChargeForYear(PrepaidExpense p, int year, decimal total, int totalDays)
    {
        var periodStart = p.StartDate.Year == year ? p.StartDate.Date : new DateTime(year, 1, 1);
        var periodEnd = p.EndDate.Year == year ? p.EndDate.Date : new DateTime(year, 12, 31);
        var days = (periodEnd - periodStart).Days + 1;
        if (days <= 0) return 0m;
        return Math.Round(total * days / totalDays, 2);
    }

    /// <summary>ยอดตัดจ่าย ณ สิ้นปีงบที่ขอ</summary>
    public static PrepaidAsOfDto AsOf(PrepaidExpense p, int fiscalYear)
    {
        var total = Math.Round(p.TotalAmount, 2);
        var schedule = BuildSchedule(p);

        if (schedule.Count == 0)
            return new PrepaidAsOfDto(0m, 0m, 0m, total, false);

        var first = schedule[0];
        var last = schedule[^1];

        if (fiscalYear < first.Year)
            return new PrepaidAsOfDto(0m, 0m, 0m, total, false);

        var row = schedule.FirstOrDefault(r => r.Year == fiscalYear);
        if (row is not null)
            return new PrepaidAsOfDto(
                row.OpeningAmortized, row.Charge, row.ClosingAmortized, row.Remaining,
                Math.Round(row.Remaining, 2) <= 0m);

        // ปีงบหลังตัดหมด — คงยอดสะสมสุดท้าย ไม่มีตัดจ่ายเพิ่ม
        return new PrepaidAsOfDto(last.ClosingAmortized, 0m, last.ClosingAmortized, last.Remaining,
            Math.Round(last.Remaining, 2) <= 0m);
    }
}
