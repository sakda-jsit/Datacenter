using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Leasing.Services;

/// <summary>
/// คำนวณตารางตัดบัญชีเช่าซื้อ/เงินกู้แบบ effective-interest (pure, ไม่แตะ DB).
///
/// หลักการ (ตรงกับ workbook 2025_JSPC_LEASING.xlsx):
/// - ค่างวด net (เงินต้น+ดอกเบี้ย) คงที่ทุกงวด → solve อัตราต่องวด r จาก
///   FinancedPrincipal = Installment · (1−(1+r)⁻ⁿ)/r  (Newton-Raphson)
/// - ดอกเบี้ยงวด k = round(ยอดเงินต้นคงเหลือ × r, 2); เงินต้นงวด k = Installment − ดอกเบี้ย
/// - งวดสุดท้าย absorb เศษปัดเศษ (เงินต้น = ยอดคงเหลือ) → ยอดรวมเงินต้น = FinancedPrincipal เป๊ะ
///   ดังนั้น Σ ดอกเบี้ย = n·Installment − FinancedPrincipal เป๊ะ (ภายในงบหนึ่งสัญญา)
/// - VAT คงที่ต่องวด (เช่าซื้อ); เงินกู้ = 0
///
/// หมายเหตุความแม่น: เลขแยกเงินต้น/ดอกเบี้ยรายงวดอาจต่างจาก Excel ต้นทาง ≤ ไม่กี่สตางค์
/// (Excel เก็บ IRR ความละเอียดเต็มและปัดเศษคนละจังหวะ) แต่ยอดรวม/ยอดสิ้นปีสอดคล้องกันภายใน.
/// </summary>
public static class LeaseAmortizationEngine
{
    /// <summary>solve อัตราต่องวด (effective) จากเงินต้น/ค่างวด/จำนวนงวด</summary>
    public static decimal SolveRatePerPeriod(decimal principal, decimal installment, int periods)
    {
        var p = (double)principal;
        var a = (double)installment;
        var n = periods;

        // ไม่มีดอกเบี้ย (ค่างวด·จำนวนงวด ≈ เงินต้น)
        if (a * n <= p + 0.005) return 0m;

        double Pv(double r) => Math.Abs(r) < 1e-12 ? a * n : a * (1 - Math.Pow(1 + r, -n)) / r;

        var rate = 0.01; // เดาเริ่มต้น 1%/งวด
        for (var i = 0; i < 200; i++)
        {
            var f = Pv(rate) - p;
            var d = (Pv(rate + 1e-7) - Pv(rate)) / 1e-7;
            if (Math.Abs(d) < 1e-15) break;
            var next = rate - f / d;
            if (next <= -0.9999) next = (rate - 0.9999) / 2; // กันหลุดช่วง
            rate = next;
            if (Math.Abs(f) < 1e-10) break;
        }
        return (decimal)rate;
    }

    /// <summary>สร้างตารางตัดบัญชีทั้งหมดของสัญญา</summary>
    public static IReadOnlyList<LeaseSchedulePeriodDto> BuildSchedule(LeaseContract c)
    {
        var n = c.NumberOfPeriods;
        var installment = c.InstallmentAmount;
        var vat = c.VatPerPeriod;
        var rate = SolveRatePerPeriod(c.FinancedPrincipal, installment, n);

        var totalInterest = Math.Round(installment * n - c.FinancedPrincipal, 2);
        var totalVat = Math.Round(vat * n, 2);
        var stepMonths = c.PaymentsPerYear > 0 ? 12 / c.PaymentsPerYear : 1;
        if (stepMonths < 1) stepMonths = 1;

        var rows = new List<LeaseSchedulePeriodDto>(n);
        var principalBal = c.FinancedPrincipal;
        var deferredBal = totalInterest;
        var vatBal = totalVat;

        for (var k = 1; k <= n; k++)
        {
            decimal interest, principal;
            if (k < n)
            {
                interest = Math.Round(principalBal * rate, 2);
                principal = installment - interest;
            }
            else
            {
                // งวดสุดท้าย: ปิดเงินต้นให้เหลือศูนย์เป๊ะ
                principal = principalBal;
                interest = installment - principal;
            }

            principalBal = Math.Round(principalBal - principal, 2);
            deferredBal = Math.Round(deferredBal - interest, 2);
            vatBal = Math.Round(vatBal - vat, 2);

            var grossInstallment = installment + vat;
            var grossLiability = Math.Round(principalBal + deferredBal + vatBal, 2);

            rows.Add(new LeaseSchedulePeriodDto(
                k,
                c.FirstInstallmentDate.AddMonths(stepMonths * (k - 1)),
                installment, principal, interest, vat, grossInstallment,
                principalBal, deferredBal, vatBal, grossLiability));
        }

        return rows;
    }

    /// <summary>สรุปยอดสิ้นปีงบจากตารางตัดบัญชี</summary>
    public static LeaseYearEndSummaryDto BuildYearEndSummary(
        IReadOnlyList<LeaseSchedulePeriodDto> schedule, int fiscalYear)
    {
        var yearStart = new DateTime(fiscalYear, 1, 1);
        var yearEnd = new DateTime(fiscalYear, 12, 31);
        var next12 = yearEnd.AddMonths(12);

        LeaseAccountBreakdownDto Breakdown(Func<LeaseSchedulePeriodDto, decimal> amount)
        {
            // opening = ยอดคงค้างต้นปี (งวดที่ครบกำหนด ≥ ต้นปี ยังไม่ชำระ)
            var opening = schedule.Where(p => p.DueDate >= yearStart).Sum(amount);
            var paid = schedule.Where(p => p.DueDate >= yearStart && p.DueDate <= yearEnd).Sum(amount);
            var closing = Math.Round(opening - paid, 2);
            var current = schedule.Where(p => p.DueDate > yearEnd && p.DueDate <= next12).Sum(amount);
            current = Math.Round(current, 2);
            return new LeaseAccountBreakdownDto(
                Math.Round(opening, 2), Math.Round(paid, 2), closing, current,
                Math.Round(closing - current, 2));
        }

        var deferred = Breakdown(p => p.Interest);

        return new LeaseYearEndSummaryDto(
            fiscalYear,
            GrossLiability: Breakdown(p => p.GrossInstallment),
            DeferredInterest: deferred,
            VatUndue: Breakdown(p => p.Vat),
            NetPrincipal: Breakdown(p => p.Principal),
            InterestRecognizedInYear: deferred.PaidInYear);
    }
}
