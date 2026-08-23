using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Services;

/// <summary>
/// นำเข้างวดเงินเดือนจาก Express — เอกสาร "บันทึกค่าใช้จ่ายอื่นๆ" (OE) ที่ลงบัญชีเงินเดือน/ค่าจ้าง.
/// แทนการ import Excel: ดึงยอดจริงต่อพนักงาน/เดือน (แยกช่อง เงินเดือน/OT/ที่พัก/อาหาร/เบี้ยขยัน/โบนัส),
/// derive ปกส.ลูกจ้าง = ปกส.รอนำส่ง(เครดิต) − เงินสมทบนายจ้าง(เดบิต), ภาษี = ภาษีหัก ณ ที่จ่ายค้างจ่าย(เครดิต).
/// เชื่อมพนักงานด้วย SUPCOD → Employee.SourceSupplierCode. ข้ามงวดที่มีอยู่แล้ว (ไม่ทับที่กรอกมือ).
/// </summary>
public static class PayrollExpressImporter
{
    public record Result(int RunsCreated, int Items, int SkippedMonths, int UnmatchedDocs, string Message);

    public static async Task<Result> ImportAsync(
        IApplicationDbContext db, IExpressDbfAdapter adapter, string folderPath,
        int companyId, string username, CancellationToken ct)
    {
        // 1) mapping บัญชี→บทบาท ต่อบริษัท (source of truth ว่าบัญชีไหนคือเงินเดือน)
        var maps = await db.PayrollAccountMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == companyId)
            .ToListAsync(ct);
        var roleByAccount = maps
            .GroupBy(m => m.AccountCode.Trim())
            .ToDictionary(g => g.Key, g => g.First().Role);

        // anchor = **เฉพาะบัญชีเงินเดือน (SalaryExpense)** — ใช้ระบุว่าเอกสาร OE ไหนเป็นเงินเดือนพนักงานจริง
        // ไม่รวม "ค่าจ้าง" (DailyWageExpense เช่น 5140-01 ค่าจ้างประกอบ) เพราะเป็น "ค่าจ้างทำของ" (ผู้รับเหมา,
        // ภ.ง.ด.3/S03 หัก 3% ไม่มี ปกส.) ไม่ใช่พนักงานเงินเดือน — จะไปเบิ้ลจำนวนพนักงาน. บัญชีคือตัวชี้
        // (TAXTYP ใน APMAS เชื่อไม่ได้: บางคน S03 แต่ลงบัญชีเงินเดือน+มี ปกส. = พนักงานจริง)
        var anchor = roleByAccount
            .Where(kv => kv.Value is PayrollPostingRole.SalaryExpense)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (anchor.Count == 0)
            return new Result(0, 0, 0, 0, "ยังไม่ได้แมพบัญชีเงินเดือน (role เงินเดือน) — ข้ามการนำเข้าเงินเดือนจาก Express");

        var lines = await adapter.ReadPayrollOeLinesAsync(folderPath, anchor, ct);
        if (lines.Count == 0)
            return new Result(0, 0, 0, 0, "ไม่พบเอกสารเงินเดือน (OE) ใน Express");

        // 2) ชื่อบัญชี (ไว้ fallback จำแนกช่องรายได้เมื่อบัญชียังไม่ถูก map role)
        var accName = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == companyId)
            .ToDictionaryAsync(a => a.AccountCode, a => a.AccountName, ct);

        // 3) พนักงานตาม SUPCOD
        var emps = await db.Employees.Where(e => e.ClientCompanyId == companyId).ToListAsync(ct);
        var empBySup = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in emps)
        {
            if (!string.IsNullOrWhiteSpace(e.SourceSupplierCode)) empBySup.TryAdd(e.SourceSupplierCode!, e);
            if (!string.IsNullOrWhiteSpace(e.EmployeeCode)) empBySup.TryAdd(e.EmployeeCode, e);
        }

        // 4) งวดที่มีอยู่แล้ว → ข้าม (ไม่ทับที่กรอกมือ/ปิดแล้ว)
        var existingMonths = (await db.PayrollRuns.AsNoTracking()
                .Where(r => r.ClientCompanyId == companyId)
                .Select(r => new { r.Year, r.Month }).ToListAsync(ct))
            .Select(x => (x.Year, x.Month)).ToHashSet();

        var rates = await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct);

        // 5) รวมบรรทัดต่อ (พนักงาน, ปี, เดือน)
        var groups = lines.GroupBy(l => (l.SupplierCode.Trim(), l.Year, l.Month));
        var acc = new Dictionary<(int Year, int Month), List<Accum>>();
        int unmatched = 0;

        foreach (var g in groups)
        {
            var (sup, year, month) = g.Key;
            if (existingMonths.Contains((year, month))) continue; // ข้ามงวดที่มีอยู่แล้ว
            if (!empBySup.TryGetValue(sup, out var emp)) { unmatched++; continue; }

            var a = new Accum { EmployeeId = emp.Id };
            foreach (var l in g)
            {
                var role = ResolveRole(l.AccountCode, l.Debit > 0, roleByAccount, accName);
                if (l.Debit > 0) ApplyDebit(a, role, l.Debit);
                else if (l.Credit > 0) ApplyCredit(a, role, l.Credit);
            }
            // ข้ามรายการว่าง (เอกสารที่บัญชีเงินเดือนอยู่ฝั่งเครดิต/กลับรายการ = ไม่มีรายได้/หักจริง)
            if (!HasData(a)) continue;
            (acc.TryGetValue((year, month), out var list) ? list : acc[(year, month)] = []).Add(a);
        }

        // 6) สร้าง PayrollRun + PayrollItem
        int runsCreated = 0, itemCount = 0;
        foreach (var ((year, month), accums) in acc.OrderBy(k => k.Key.Year).ThenBy(k => k.Key.Month))
        {
            var cfg = PayrollRates.ResolveEffective(rates, new DateTime(year, month, 1));
            var empPct = cfg?.SsoEmployeePct ?? 0m;

            var run = new PayrollRun
            {
                ClientCompanyId = companyId, Year = year, Month = month,
                Status = PayrollRunStatus.Draft, CreatedBy = username,
                Note = "นำเข้าจาก Express (บันทึกค่าใช้จ่ายอื่นๆ)",
            };
            foreach (var a in accums)
            {
                var empSso = Math.Max(a.SsoPayable - a.EmployerSso, 0m);
                var item = new PayrollItem
                {
                    EmployeeId = a.EmployeeId,
                    Salary = a.Salary,
                    DailyWageRate = a.DailyWage,
                    DailyWageDays = a.DailyWage > 0 ? 1 : 0, // Express เก็บยอดรวม ไม่มีวัน×เรท → ลงเป็นก้อน
                    Overtime = a.Overtime,
                    HousingAllowance = a.Housing,
                    FoodAllowance = a.Food,
                    Diligence = a.Diligence,
                    Bonus = a.Bonus,
                    OtherIncome = a.OtherIncome,
                    SsoEmployee = empSso,
                    // ฐานยื่น ปกส. = ถอดกลับจากยอดหักจริง/อัตรา (ให้ตัวเทียบ ปกส.ตรง); ไม่มีอัตรา → 0
                    SsoWageBase = empSso > 0 && empPct > 0 ? PayrollCalculator.Round2(empSso / (empPct / 100m)) : 0m,
                    WithholdingTax = a.Wht,
                    OtherDeduction = a.OtherDeduction,
                    CreatedBy = username,
                    Note = a.DailyWage > 0 ? "ค่าจ้างรายวันลงเป็นยอดรวม (Express)" : null,
                };
                PayrollCalculator.Recompute(item);
                run.Items.Add(item);
                itemCount++;
            }
            if (run.Items.Count == 0) continue;
            db.PayrollRuns.Add(run);
            runsCreated++;
        }

        if (runsCreated > 0) await db.SaveChangesAsync(ct);

        var skipped = acc.Count == 0 && existingMonths.Count > 0 ? existingMonths.Count : 0;
        var msg = $"นำเข้าเงินเดือนจาก Express: {runsCreated} งวด, {itemCount} รายการ"
                + (unmatched > 0 ? $" (ข้าม {unmatched} เอกสารที่จับคู่พนักงานไม่ได้)" : "");
        return new Result(runsCreated, itemCount, skipped, unmatched, msg);
    }

    private sealed class Accum
    {
        public int EmployeeId;
        public decimal Salary, DailyWage, Overtime, Housing, Food, Diligence, Bonus, OtherIncome;
        public decimal EmployerSso, SsoPayable, Wht, OtherDeduction;
    }

    // มีข้อมูลจริงก็ต่อเมื่อพนักงานได้รับ/ถูกหักอะไรจริง — ไม่นับ EmployerSso/SsoPayable เดี่ยว ๆ
    // (เอกสารกลับรายการ/ตั้งค้างที่บัญชีเงินเดือนอยู่ฝั่งเครดิต + สมทบนายจ้าง = ไม่ใช่งวดจ่ายจริง)
    private static bool HasData(Accum a)
    {
        var income = a.Salary + a.DailyWage + a.Overtime + a.Housing + a.Food + a.Diligence + a.Bonus + a.OtherIncome;
        var empSso = Math.Max(a.SsoPayable - a.EmployerSso, 0m);
        return income + empSso + a.Wht + a.OtherDeduction != 0m;
    }

    private static void ApplyDebit(Accum a, PayrollPostingRole? role, decimal amt)
    {
        switch (role)
        {
            case PayrollPostingRole.SalaryExpense:          a.Salary += amt; break;
            case PayrollPostingRole.DailyWageExpense:       a.DailyWage += amt; break;
            case PayrollPostingRole.OvertimeExpense:        a.Overtime += amt; break;
            case PayrollPostingRole.HousingAllowanceExpense: a.Housing += amt; break;
            case PayrollPostingRole.FoodAllowanceExpense:   a.Food += amt; break;
            case PayrollPostingRole.DiligenceExpense:       a.Diligence += amt; break;
            case PayrollPostingRole.BonusExpense:           a.Bonus += amt; break;
            case PayrollPostingRole.EmployerSsoExpense:     a.EmployerSso += amt; break;
            default:                                        a.OtherIncome += amt; break; // Allowance รวม/ไม่รู้จัก
        }
    }

    private static void ApplyCredit(Accum a, PayrollPostingRole? role, decimal amt)
    {
        switch (role)
        {
            case PayrollPostingRole.SsoPayable:             a.SsoPayable += amt; break;
            case PayrollPostingRole.WhtPayable:             a.Wht += amt; break;
            case PayrollPostingRole.EmployeeDeductionCredit: a.OtherDeduction += amt; break;
            // NetPayCredit / เครดิตอื่น (เงินสด/แบงก์) = ตัวปิดยอด ไม่ตั้งเป็น field (คำนวณ Net จากรายได้−หัก)
            default: break;
        }
    }

    /// <summary>หา role ของบัญชี: mapping ก่อน ถ้าไม่มีให้เดาจากชื่อบัญชี (คนละบัญชีต่อ component ใน Express)</summary>
    private static PayrollPostingRole? ResolveRole(
        string accountCode, bool isDebit,
        IReadOnlyDictionary<string, PayrollPostingRole> roleByAccount,
        IReadOnlyDictionary<string, string> accName)
    {
        if (roleByAccount.TryGetValue(accountCode.Trim(), out var r)) return r;

        var name = accName.GetValueOrDefault(accountCode, "");
        if (name.Length == 0) return null;

        if (isDebit)
        {
            if (name.Contains("ล่วงเวลา") || name.Contains("โอที")) return PayrollPostingRole.OvertimeExpense;
            if (name.Contains("ที่พัก")) return PayrollPostingRole.HousingAllowanceExpense;
            if (name.Contains("อาหาร")) return PayrollPostingRole.FoodAllowanceExpense;
            if (name.Contains("ขยัน")) return PayrollPostingRole.DiligenceExpense;
            if (name.Contains("โบนัส")) return PayrollPostingRole.BonusExpense;
            if (name.Contains("สมทบ") && name.Contains("ประกันสังคม")) return PayrollPostingRole.EmployerSsoExpense;
            if (name.Contains("เงินเดือน")) return PayrollPostingRole.SalaryExpense;
            if (name.Contains("ค่าจ้าง")) return PayrollPostingRole.DailyWageExpense;
        }
        else
        {
            if (name.Contains("ประกันสังคม")) return PayrollPostingRole.SsoPayable;
            if (name.Contains("ภาษีหัก") || name.Contains("ณ ที่จ่าย")) return PayrollPostingRole.WhtPayable;
        }
        return null;
    }
}
