using Datacenter.Application.Features.InterestIncome.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.InterestIncome.Services;

/// <summary>
/// คำนวณดอกเบี้ยรับตามยอดเงินต้นคงเหลือรายช่วง (pure — ไม่แตะ DB, req v11 docs/13 INTEREST INCOME).
///
/// หลักการ (ตรง spec "ดอกเบี้ย = เงินต้นคงเหลือ × อัตรา × จำนวนวัน / ฐานวันต่อปี"):
/// - ยอดเงินต้นเปลี่ยนตาม movement (Balance = ผลรวมจำนวนถึงวันนั้น)
/// - แบ่งช่วงภายในปีงบที่ยอดเงินต้นคงที่ → ดอกเบี้ยช่วง = round(balance × rate% × days / basis, 2)
/// - นับวันแบบ [from, nextChange) (exclusive ปลาย) เพื่อไม่ให้นับวันซ้ำที่จุดเปลี่ยน; ช่วงสุดท้ายถึง 31 ธ.ค.
/// - ภาษีธุรกิจเฉพาะ (SBT) = ดอกเบี้ย × SbtRatePct%; ส่วนท้องถิ่น = SBT × LocalTaxPctOfSbt%
/// </summary>
public static class InterestIncomeEngine
{
    /// <summary>สร้างช่วงดอกเบี้ยภายในปีงบ</summary>
    public static IReadOnlyList<InterestSegmentDto> BuildSegments(InterestBearingLoan loan, int fiscalYear)
    {
        var segments = new List<InterestSegmentDto>();
        var rate = loan.AnnualRatePct / 100m;
        var basis = loan.DayCountBasis > 0 ? loan.DayCountBasis : 365;

        var yearStart = new DateTime(fiscalYear, 1, 1);
        var yearEnd = new DateTime(fiscalYear, 12, 31);

        var movements = loan.Movements.OrderBy(m => m.Date).ToList();

        // เงินต้นต้นปี = ผลรวม movement ก่อนวันที่ 1 ม.ค. ปีงบ
        var balance = movements.Where(m => m.Date.Date < yearStart).Sum(m => m.Amount);

        // จุดเปลี่ยนภายในปี (รวมยอดต่อวัน)
        var changes = movements
            .Where(m => m.Date.Date >= yearStart && m.Date.Date <= yearEnd)
            .GroupBy(m => m.Date.Date)
            .Select(g => new { Date = g.Key, Delta = g.Sum(x => x.Amount) })
            .OrderBy(x => x.Date)
            .ToList();

        var cursor = yearStart;
        foreach (var ch in changes)
        {
            var days = (ch.Date - cursor).Days; // วันที่ยอดคงเดิม ก่อนการเปลี่ยน
            if (days > 0)
                segments.Add(MakeSegment(cursor, ch.Date.AddDays(-1), balance, days, rate, basis));
            balance += ch.Delta;
            cursor = ch.Date;
        }

        var lastDays = (yearEnd.AddDays(1) - cursor).Days;
        if (lastDays > 0)
            segments.Add(MakeSegment(cursor, yearEnd, balance, lastDays, rate, basis));

        return segments;
    }

    private static InterestSegmentDto MakeSegment(
        DateTime from, DateTime to, decimal balance, int days, decimal rate, int basis)
    {
        var interest = balance > 0m ? Math.Round(balance * rate * days / basis, 2) : 0m;
        return new InterestSegmentDto(from, to, Math.Round(balance, 2), days, interest);
    }

    /// <summary>ยอด ณ ปีงบ: เงินต้นต้นปี/ปลายปี + ดอกเบี้ยรับในปี + SBT + ส่วนท้องถิ่น</summary>
    public static InterestAsOfDto AsOf(InterestBearingLoan loan, int fiscalYear)
    {
        var yearStart = new DateTime(fiscalYear, 1, 1);
        var yearEnd = new DateTime(fiscalYear, 12, 31);

        var opening = Math.Round(loan.Movements.Where(m => m.Date.Date < yearStart).Sum(m => m.Amount), 2);
        var closing = Math.Round(loan.Movements.Where(m => m.Date.Date <= yearEnd).Sum(m => m.Amount), 2);

        var interest = BuildSegments(loan, fiscalYear).Sum(s => s.Interest);
        interest = Math.Round(interest, 2);

        var sbt = Math.Round(interest * loan.SbtRatePct / 100m, 2);
        var localTax = Math.Round(sbt * loan.LocalTaxPctOfSbt / 100m, 2);

        return new InterestAsOfDto(opening, closing, interest, sbt, localTax, Math.Round(sbt + localTax, 2));
    }
}
