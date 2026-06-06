using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Payroll.Services;

/// <summary>
/// ตัวคำนวณ ปกส./ภาษี เป็น "ตัวเทียบ" (cross-check) กับตัวเลขจริงจาก slip — pure, ไม่แตะ DB.
/// ภาษี = ประมาณการแบบ annualize (เงินได้เดือนนั้น × 12) ตามขั้นบันได PIT มาตรฐาน.
/// </summary>
public static class PayrollCalculator
{
    public static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>รวมรายได้ = เงินเดือน + (วัน×ค่าจ้างวัน) + เบี้ยเลี้ยง/OT/โบนัส/อื่น</summary>
    public static decimal Gross(PayrollItem it) => Round2(
        it.Salary + it.DailyWageDays * it.DailyWageRate + it.HousingAllowance + it.FoodAllowance
        + it.Overtime + it.Diligence + it.Bonus + it.OtherIncome);

    /// <summary>เงินสุทธิ = รวมรายได้ − ขาดงาน − ปกส. − ภาษี − หักอื่น</summary>
    public static decimal Net(PayrollItem it) => Round2(
        it.GrossIncome - it.Absence - it.SsoEmployee - it.WithholdingTax - it.OtherDeduction);

    /// <summary>คำนวณ GrossIncome แล้วตามด้วย NetPay (เรียกหลังแก้ค่า)</summary>
    public static void Recompute(PayrollItem it)
    {
        it.GrossIncome = Gross(it);
        it.NetPay = Net(it);
    }

    /// <summary>เงินสมทบ ปกส. = clamp(ฐานค่าจ้าง, floor, cap) × อัตรา%</summary>
    public static decimal Sso(decimal wageBase, decimal pct, decimal floor, decimal cap)
    {
        var b = Math.Min(Math.Max(wageBase, floor), cap);
        if (wageBase <= 0) b = 0; // ไม่มีค่าจ้าง = ไม่สมทบ
        return Round2(b * pct / 100m);
    }

    public static decimal SsoEmployee(PayrollItem it, PayrollRateConfig? c)
        => c is null ? 0 : Sso(it.SsoWageBase, c.SsoEmployeePct, c.SsoWageFloor, c.SsoWageCap);

    public static decimal SsoEmployer(PayrollItem it, PayrollRateConfig? c)
        => c is null ? 0 : Sso(it.SsoWageBase, c.SsoEmployerPct, c.SsoWageFloor, c.SsoWageCap);

    /// <summary>ประมาณภาษีหัก ณ ที่จ่ายต่อเดือน (annualize เงินได้เดือนนั้น) — ตัวเทียบ</summary>
    public static decimal MonthlyTaxEstimate(decimal monthlyTaxableIncome, decimal monthlySso)
    {
        if (monthlyTaxableIncome <= 0) return 0;
        var annual = monthlyTaxableIncome * 12m;
        var expense = Math.Min(annual * 0.5m, 100000m);        // หักค่าใช้จ่าย 50% ไม่เกิน 100,000
        var personal = 60000m;                                  // ลดหย่อนส่วนตัว
        var ssoAnnual = Math.Min(monthlySso * 12m, 9000m);      // ปกส. ลดหย่อนสูงสุด 9,000/ปี
        var net = annual - expense - personal - ssoAnnual;
        var annualTax = ProgressiveTax(net);
        return Round2(annualTax / 12m);
    }

    /// <summary>ภาษีเงินได้บุคคลธรรมดาแบบขั้นบันได (โครงสร้าง 2560+)</summary>
    public static decimal ProgressiveTax(decimal net)
    {
        if (net <= 150000m) return 0m;
        decimal[][] br =
        [
            [150000m, 300000m, 0.05m],
            [300000m, 500000m, 0.10m],
            [500000m, 750000m, 0.15m],
            [750000m, 1000000m, 0.20m],
            [1000000m, 2000000m, 0.25m],
            [2000000m, 5000000m, 0.30m],
        ];
        decimal tax = 0m;
        foreach (var b in br)
        {
            if (net <= b[0]) break;
            var amt = Math.Min(net, b[1]) - b[0];
            tax += amt * b[2];
        }
        if (net > 5000000m) tax += (net - 5000000m) * 0.35m;
        return tax;
    }
}
