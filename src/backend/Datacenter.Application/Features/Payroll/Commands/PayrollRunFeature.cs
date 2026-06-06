using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── รายการงวด ────────────────────────────────────────────────────────────────────
public record GetPayrollRunsQuery(int ClientCompanyId, int? Year = null)
    : IRequest<IReadOnlyList<PayrollRunListItemDto>>, IRequireCompanyAccess;

public class GetPayrollRunsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollRunsQuery, IReadOnlyList<PayrollRunListItemDto>>
{
    public async Task<IReadOnlyList<PayrollRunListItemDto>> Handle(GetPayrollRunsQuery request, CancellationToken ct)
    {
        var q = db.PayrollRuns.AsNoTracking().Include(r => r.Items)
            .Where(r => r.ClientCompanyId == request.ClientCompanyId);
        if (request.Year is { } y) q = q.Where(r => r.Year == y);
        var runs = await q.OrderByDescending(r => r.Year).ThenByDescending(r => r.Month).ToListAsync(ct);
        return runs.Select(r => new PayrollRunListItemDto(
            r.Id, r.Year, r.Month, (int)r.Status, r.Items.Count,
            r.Items.Sum(i => i.GrossIncome), r.Items.Sum(i => i.SsoEmployee),
            r.Items.Sum(i => i.WithholdingTax), r.Items.Sum(i => i.NetPay))).ToList();
    }
}

// ── รายละเอียดงวด + ค่าคำนวณเทียบ ────────────────────────────────────────────────
public record GetPayrollRunQuery(int ClientCompanyId, int RunId)
    : IRequest<PayrollRunDetailDto>, IRequireCompanyAccess;

public class GetPayrollRunQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollRunQuery, PayrollRunDetailDto>
{
    public async Task<PayrollRunDetailDto> Handle(GetPayrollRunQuery request, CancellationToken ct)
    {
        var run = await db.PayrollRuns.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);

        var asOf = new DateTime(run.Year, run.Month, 1);
        var allRates = await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct);
        var cfg = PayrollRates.ResolveEffective(allRates, asOf);

        var items = run.Items.OrderBy(i => i.Employee!.EmployeeCode).Select(i =>
        {
            var ssoEmpCalc = PayrollCalculator.SsoEmployee(i, cfg);
            var ssoErCalc = PayrollCalculator.SsoEmployer(i, cfg);
            var taxCalc = PayrollCalculator.MonthlyTaxEstimate(i.GrossIncome, i.SsoEmployee);
            return new PayrollItemDto(
                i.Id, i.EmployeeId, i.Employee!.EmployeeCode,
                ((i.Employee.Prefix ?? "") + " " + i.Employee.FirstName + " " + i.Employee.LastName).Trim(),
                i.Employee.Department,
                (int)i.Employee.SalaryType,
                i.Salary, i.DailyWageDays, i.DailyWageRate, i.HousingAllowance, i.FoodAllowance, i.Overtime,
                i.Diligence, i.Bonus, i.OtherIncome, i.GrossIncome, i.SsoWageBase, i.SsoEmployee, i.WithholdingTax,
                i.Absence, i.Advance, i.OtherDeduction, i.NetPay,
                ssoEmpCalc, ssoErCalc, PayrollCalculator.Round2(i.SsoEmployee - ssoEmpCalc),
                taxCalc, PayrollCalculator.Round2(i.WithholdingTax - taxCalc), i.Note);
        }).ToList();

        return new PayrollRunDetailDto(
            run.Id, run.ClientCompanyId, run.Year, run.Month, (int)run.Status, run.Note,
            cfg?.SsoEmployeePct, cfg?.SsoEmployerPct, cfg?.SsoWageFloor, cfg?.SsoWageCap,
            items,
            items.Sum(x => x.GrossIncome), items.Sum(x => x.SsoEmployee), items.Sum(x => x.SsoEmployerCalc),
            items.Sum(x => x.WithholdingTax), items.Sum(x => x.NetPay));
    }
}

// ── สรุปรายได้ทั้งปี (แถว=เดือน) อิง sheet "รายได้ทั้งปี" ─────────────────────────
public record GetPayrollYearSummaryQuery(int ClientCompanyId, int Year)
    : IRequest<PayrollYearSummaryDto>, IRequireCompanyAccess;

public class GetPayrollYearSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollYearSummaryQuery, PayrollYearSummaryDto>
{
    private const decimal WcfMonthlyCap = 20000m; // ฐานกองทุนทดแทน (กท.20ก) เพดาน 20,000/คน/เดือน

    public async Task<PayrollYearSummaryDto> Handle(GetPayrollYearSummaryQuery request, CancellationToken ct)
    {
        var runs = await db.PayrollRuns.AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.ClientCompanyId == request.ClientCompanyId && r.Year == request.Year)
            .ToListAsync(ct);
        var allRates = await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct);

        var byMonth = runs.GroupBy(r => r.Month).ToDictionary(g => g.Key, g => g.First());
        var months = new List<PayrollSummaryRow>(12);
        for (int m = 1; m <= 12; m++)
        {
            if (byMonth.TryGetValue(m, out var run))
            {
                var cfg = PayrollRates.ResolveEffective(allRates, new DateTime(request.Year, m, 1));
                months.Add(Aggregate(m, run.Items, cfg));
            }
            else months.Add(EmptyRow(m));
        }

        var total = SumRows(months);
        return new PayrollYearSummaryDto(request.Year, months, total);
    }

    private static PayrollSummaryRow Aggregate(int month, IEnumerable<PayrollItem> items, PayrollRateConfig? cfg)
    {
        decimal salary = 0, absence = 0, housing = 0, food = 0, ot = 0, diligence = 0, bonus = 0,
            totalIncome = 0, wage = 0, wageOver = 0, ssoReportable = 0, ssoCalc = 0, ssoActual = 0,
            tax = 0, advance = 0, employerSso = 0;
        int count = 0;
        foreach (var i in items)
        {
            count++;
            // เงินเดือน + ค่าจ้างรายวัน = รวมรายได้ − เบี้ยเลี้ยง/OT/อื่น (อิงยอดที่บันทึกจริง; วัน×เรท อาจปัดเศษไม่ตรง)
            salary += i.GrossIncome - i.HousingAllowance - i.FoodAllowance - i.Overtime - i.Diligence - i.Bonus - i.OtherIncome;
            absence += i.Absence;
            housing += i.HousingAllowance;
            food += i.FoodAllowance;
            ot += i.Overtime;
            diligence += i.Diligence;
            bonus += i.Bonus;
            totalIncome += i.GrossIncome;
            wage += i.SsoWageBase;
            wageOver += Math.Max(i.SsoWageBase - WcfMonthlyCap, 0m);
            ssoReportable += i.SsoWageBase;
            ssoCalc += PayrollCalculator.SsoEmployee(i, cfg);
            ssoActual += i.SsoEmployee;
            tax += i.WithholdingTax;
            advance += i.Advance;
            employerSso += PayrollCalculator.SsoEmployer(i, cfg);
        }
        var netSalary = salary - absence;
        var netAfterAbsence = totalIncome - absence;                 // รายได้สุทธิหลังหักลา (ภงด.1ก)
        var totalDeduction = absence + ssoActual + tax + advance;    // รวมรายการหัก
        var shortfall = Math.Max(ssoCalc - ssoActual, 0m);           // ปกส.ขาดไป
        return new PayrollSummaryRow(
            month, count, true,
            PayrollCalculator.Round2(salary), PayrollCalculator.Round2(absence), PayrollCalculator.Round2(netSalary),
            PayrollCalculator.Round2(housing), PayrollCalculator.Round2(food), PayrollCalculator.Round2(ot),
            PayrollCalculator.Round2(diligence), PayrollCalculator.Round2(bonus),
            PayrollCalculator.Round2(netAfterAbsence), PayrollCalculator.Round2(totalIncome),
            PayrollCalculator.Round2(wage), PayrollCalculator.Round2(wageOver),
            PayrollCalculator.Round2(ssoReportable), PayrollCalculator.Round2(ssoCalc),
            PayrollCalculator.Round2(shortfall), PayrollCalculator.Round2(ssoActual),
            PayrollCalculator.Round2(tax), PayrollCalculator.Round2(absence), PayrollCalculator.Round2(advance),
            PayrollCalculator.Round2(totalDeduction), PayrollCalculator.Round2(netAfterAbsence),
            PayrollCalculator.Round2(employerSso), PayrollCalculator.Round2(totalIncome - totalDeduction));
    }

    private static PayrollSummaryRow EmptyRow(int month) =>
        new(month, 0, false, 0,0,0, 0,0,0,0,0, 0,0, 0,0, 0,0,0,0, 0,0,0, 0,0,0,0);

    private static PayrollSummaryRow SumRows(IReadOnlyList<PayrollSummaryRow> rows)
    {
        PayrollSummaryRow t = new(0, 0, true, 0,0,0, 0,0,0,0,0, 0,0, 0,0, 0,0,0,0, 0,0,0, 0,0,0,0);
        foreach (var r in rows)
            t = t with {
                EmployeeCount = Math.Max(t.EmployeeCount, r.EmployeeCount),
                Salary = t.Salary + r.Salary, AbsenceLate = t.AbsenceLate + r.AbsenceLate, NetSalary = t.NetSalary + r.NetSalary,
                Housing = t.Housing + r.Housing, Food = t.Food + r.Food, Overtime = t.Overtime + r.Overtime,
                Diligence = t.Diligence + r.Diligence, Bonus = t.Bonus + r.Bonus,
                NetIncomeAfterAbsence = t.NetIncomeAfterAbsence + r.NetIncomeAfterAbsence, TotalIncome = t.TotalIncome + r.TotalIncome,
                Wage = t.Wage + r.Wage, WageOver20000 = t.WageOver20000 + r.WageOver20000,
                SsoReportable = t.SsoReportable + r.SsoReportable, SsoCalc = t.SsoCalc + r.SsoCalc,
                SsoShortfall = t.SsoShortfall + r.SsoShortfall, SsoActual = t.SsoActual + r.SsoActual,
                Tax = t.Tax + r.Tax, Absence = t.Absence + r.Absence, Advance = t.Advance + r.Advance,
                TotalDeduction = t.TotalDeduction + r.TotalDeduction, Pnd1Income = t.Pnd1Income + r.Pnd1Income,
                EmployerSso = t.EmployerSso + r.EmployerSso, NetPay = t.NetPay + r.NetPay,
            };
        return t;
    }
}

// ── สร้างงวด + auto สร้างรายการจากพนักงาน Active ─────────────────────────────────
public record CreatePayrollRunCommand(int ClientCompanyId, int Year, int Month)
    : IRequest<int>, IRequireCompanyAccess;

public class CreatePayrollRunCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreatePayrollRunCommand, int>
{
    public async Task<int> Handle(CreatePayrollRunCommand request, CancellationToken ct)
    {
        if (request.Month is < 1 or > 12)
            throw new Datacenter.Application.Common.Exceptions.ValidationException(new[] {
                new FluentValidation.Results.ValidationFailure("Month", "เดือนต้องอยู่ระหว่าง 1-12") });

        var exists = await db.PayrollRuns.AnyAsync(
            r => r.ClientCompanyId == request.ClientCompanyId && r.Year == request.Year && r.Month == request.Month, ct);
        if (exists)
            throw new Datacenter.Application.Common.Exceptions.ValidationException(new[] {
                new FluentValidation.Results.ValidationFailure("Month", $"มีงวด {request.Month}/{request.Year} อยู่แล้ว") });

        var run = new PayrollRun
        {
            ClientCompanyId = request.ClientCompanyId, Year = request.Year, Month = request.Month,
            Status = PayrollRunStatus.Draft, CreatedBy = currentUser.Username,
        };

        // prefill จากพนักงานที่ยัง Active (เงินเดือน/ค่าจ้างวันจากทะเบียน; ฐาน ปกส. = เพดานเงินเดือน)
        var emps = await db.Employees.AsNoTracking()
            .Where(e => e.ClientCompanyId == request.ClientCompanyId && e.EmploymentStatus == EmploymentStatus.Active)
            .ToListAsync(ct);
        foreach (var e in emps)
        {
            var item = new PayrollItem
            {
                EmployeeId = e.Id,
                Salary = e.SalaryType == SalaryType.Monthly ? e.BaseSalary : 0,
                DailyWageRate = e.DailyWage ?? 0,
                CreatedBy = currentUser.Username,
            };
            item.SsoWageBase = item.Salary; // ตั้งต้น = เงินเดือน (แก้ได้ตอนกรอก)
            PayrollCalculator.Recompute(item);
            run.Items.Add(item);
        }

        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run.Id;
    }
}

// ── บันทึกค่ารายการ (grid save) — recompute Gross/Net ────────────────────────────
public record SavePayrollItemsCommand(int ClientCompanyId, int RunId, IReadOnlyList<PayrollItemInput> Items)
    : IRequest<Unit>, IRequireCompanyAccess;

public class SavePayrollItemsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SavePayrollItemsCommand, Unit>
{
    public async Task<Unit> Handle(SavePayrollItemsCommand request, CancellationToken ct)
    {
        var run = await db.PayrollRuns.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);

        var byId = run.Items.ToDictionary(i => i.Id);
        foreach (var inp in request.Items)
        {
            if (!byId.TryGetValue(inp.Id, out var it)) continue;
            it.Salary = inp.Salary; it.DailyWageDays = inp.DailyWageDays; it.DailyWageRate = inp.DailyWageRate;
            it.HousingAllowance = inp.HousingAllowance; it.FoodAllowance = inp.FoodAllowance; it.Overtime = inp.Overtime;
            it.Diligence = inp.Diligence; it.Bonus = inp.Bonus; it.OtherIncome = inp.OtherIncome;
            it.SsoWageBase = inp.SsoWageBase; it.SsoEmployee = inp.SsoEmployee; it.WithholdingTax = inp.WithholdingTax;
            it.Absence = inp.Absence; it.Advance = inp.Advance; it.OtherDeduction = inp.OtherDeduction; it.Note = inp.Note;
            PayrollCalculator.Recompute(it);
        }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── ลบงวด ────────────────────────────────────────────────────────────────────────
public record DeletePayrollRunCommand(int ClientCompanyId, int RunId)
    : IRequest<Unit>, IRequireCompanyAccess;

public class DeletePayrollRunCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePayrollRunCommand, Unit>
{
    public async Task<Unit> Handle(DeletePayrollRunCommand request, CancellationToken ct)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(
            r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);
        db.PayrollRuns.Remove(run);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── เปลี่ยนสถานะงวด ──────────────────────────────────────────────────────────────
public record SetPayrollRunStatusCommand(int ClientCompanyId, int RunId, int Status)
    : IRequest<Unit>, IRequireCompanyAccess;

public class SetPayrollRunStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SetPayrollRunStatusCommand, Unit>
{
    public async Task<Unit> Handle(SetPayrollRunStatusCommand request, CancellationToken ct)
    {
        var run = await db.PayrollRuns.FirstOrDefaultAsync(
            r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);
        run.Status = (PayrollRunStatus)request.Status;
        run.ModifiedBy = currentUser.Username;
        run.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
