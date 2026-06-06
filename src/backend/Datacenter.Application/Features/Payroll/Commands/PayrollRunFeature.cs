using Datacenter.Application.Common;
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
            employerSso += i.SsoEmployee; // นายจ้างสมทบ = ยอดหักลูกจ้างจริง (จับคู่ 1:1 ตามกฎ ปกส.)
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

// ── สปส.1-10 (แบบรายการแสดงการส่งเงินสมทบ) ต่องวด ────────────────────────────────
public record GetSsoFilingQuery(int ClientCompanyId, int RunId)
    : IRequest<SsoFilingDto>, IRequireCompanyAccess;

public class GetSsoFilingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSsoFilingQuery, SsoFilingDto>
{
    public async Task<SsoFilingDto> Handle(GetSsoFilingQuery request, CancellationToken ct)
    {
        var run = await db.PayrollRuns.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);

        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var cfg = PayrollRates.ResolveEffective(
            await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct), new DateTime(run.Year, run.Month, 1));

        // เฉพาะผู้ประกันตนที่มีค่าจ้างยื่น ปกส.เดือนนั้น เรียงตามเลขบัตร ปชช.
        var rows = run.Items
            .Where(i => i.SsoWageBase > 0 && i.Employee != null)
            .Select(i => new
            {
                Nat = Digits(i.Employee!.NationalId),
                i.Employee.Prefix, i.Employee.FirstName, i.Employee.LastName,
                Wage = PayrollCalculator.Round2(i.SsoWageBase),
                Contribution = PayrollCalculator.Round2(i.SsoEmployee),
            })
            .OrderBy(x => x.Nat, StringComparer.Ordinal)
            .Select((x, idx) => new SsoFilingRow(
                idx + 1, x.Nat, x.Prefix ?? "", x.FirstName, x.LastName, x.Wage, x.Contribution))
            .ToList();

        var totalWage = rows.Sum(r => r.Wage);
        var totalEmp = rows.Sum(r => r.Contribution);
        var grand = totalEmp * 2; // นายจ้างสมทบเท่าลูกจ้าง

        // สถานะการยื่น + ใบเสร็จ (ถ้ามี) + กระทบยอด
        var filing = await db.SsoMonthlyFilings.AsNoTracking()
            .FirstOrDefaultAsync(f => f.PayrollRunId == run.Id, ct);
        SsoFilingStatusDto? status = filing is null ? null : new SsoFilingStatusDto(
            (int)filing.Status, filing.SubmittedDate,
            filing.ReceiptDate, filing.ReceiptAmount, filing.ReceiptNo, filing.Note,
            HasForm: filing.FormContent != null && filing.FormContent.Length > 0,
            HasReceipt: filing.ReceiptContent != null && filing.ReceiptContent.Length > 0,
            PayrollMatch: filing.SubmittedDate == null || Math.Abs(filing.SnapshotGrandTotal - grand) < 0.05m,
            ReceiptMatch: filing.ReceiptAmount.HasValue && Math.Abs(filing.ReceiptAmount.Value - grand) < 0.05m,
            SnapshotGrandTotal: filing.SnapshotGrandTotal);

        return new SsoFilingDto(
            run.Id, run.Year, run.Month,
            string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            company.Address, company.PostalCode, company.Phone,
            company.SsoAccountNo ?? "", company.SsoBranchCode ?? "000000", cfg?.SsoEmployeePct ?? 0,
            rows, totalWage, totalEmp, totalEmp, grand, rows.Count, ThaiBahtText.Convert(grand), status);
    }

    private static string Digits(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\D", "");
}

public record GetSsoFilingExcelQuery(int ClientCompanyId, int RunId) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetSsoFilingExcelQueryHandler(ISender sender, ISsoFilingExcelService excel)
    : IRequestHandler<GetSsoFilingExcelQuery, byte[]>
{
    public async Task<byte[]> Handle(GetSsoFilingExcelQuery req, CancellationToken ct)
        => excel.BuildEServiceFile(await sender.Send(new GetSsoFilingQuery(req.ClientCompanyId, req.RunId), ct));
}

public record GetSsoFilingPdfQuery(int ClientCompanyId, int RunId) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetSsoFilingPdfQueryHandler(ISender sender, ISsoFilingPdfService pdf)
    : IRequestHandler<GetSsoFilingPdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetSsoFilingPdfQuery req, CancellationToken ct)
        => pdf.Generate(await sender.Send(new GetSsoFilingQuery(req.ClientCompanyId, req.RunId), ct));
}

// ── ใบสำคัญลงบัญชีเงินเดือน + กระทบยอด GL (ไม่โพสต์ทับ GL ที่ import จาก Express) ──
public record GetPayrollPostingQuery(int ClientCompanyId, int RunId)
    : IRequest<PayrollPostingDto>, IRequireCompanyAccess;

public class GetPayrollPostingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollPostingQuery, PayrollPostingDto>
{
    private static readonly Dictionary<PayrollPostingRole, string> RoleLabels = new()
    {
        [PayrollPostingRole.SalaryExpense] = "เงินเดือน",
        [PayrollPostingRole.DailyWageExpense] = "ค่าจ้างรายวัน",
        [PayrollPostingRole.AllowanceExpense] = "เบี้ยเลี้ยง/OT/โบนัส",
        [PayrollPostingRole.EmployerSsoExpense] = "เงินสมทบนายจ้าง ปกส.",
        [PayrollPostingRole.SsoPayable] = "เงินประกันสังคมรอนำส่ง",
        [PayrollPostingRole.WhtPayable] = "ภาษีหัก ณ ที่จ่ายค้างจ่าย",
        [PayrollPostingRole.EmployeeDeductionCredit] = "หักจากพนักงาน (ขาดงาน/เบิกล่วงหน้า/อื่น)",
        [PayrollPostingRole.NetPayCredit] = "เงินเดือน/ค่าจ้างสุทธิค้างจ่าย",
    };

    public async Task<PayrollPostingDto> Handle(GetPayrollPostingQuery req, CancellationToken ct)
    {
        var run = await db.PayrollRuns.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .FirstOrDefaultAsync(r => r.Id == req.RunId && r.ClientCompanyId == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", req.RunId);

        var maps = await db.PayrollAccountMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == req.ClientCompanyId).ToListAsync(ct);
        var mapDict = maps.ToDictionary(m => (m.Role, m.Department ?? ""), m => m);
        var accNames = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == req.ClientCompanyId)
            .ToDictionaryAsync(a => a.AccountCode, a => a.AccountName, ct);

        // ความเคลื่อนไหวจริงใน GL (debit-credit) ต่อบัญชี เดือนนั้น
        var mStart = new DateTime(run.Year, run.Month, 1);
        var mEnd = mStart.AddMonths(1);
        var glMove = await (from l in db.JournalEntryLines.AsNoTracking()
                            join e in db.JournalEntries.AsNoTracking() on l.JournalEntryId equals e.Id
                            join a in db.Accounts.AsNoTracking() on l.AccountId equals a.Id
                            where e.ClientCompanyId == req.ClientCompanyId && e.JournalDate >= mStart && e.JournalDate < mEnd
                            group (l.DebitAmount - l.CreditAmount) by a.AccountCode into g
                            select new { Code = g.Key, Move = g.Sum() })
                           .ToDictionaryAsync(x => x.Code, x => x.Move, ct);

        var byDept = run.Items.GroupBy(i => string.IsNullOrWhiteSpace(i.Employee!.Department) ? "ไม่ระบุฝ่าย" : i.Employee!.Department!)
            .OrderBy(g => rankDept(g.Key)).ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        var lines = new List<PayrollPostingLine>();
        var warnings = new List<string>();
        var expectedByAcc = new Dictionary<string, decimal>();

        bool Has(PayrollPostingRole role, string? dept) => mapDict.ContainsKey((role, dept ?? ""));
        void Add(PayrollPostingRole role, string? dept, decimal debit, decimal credit)
        {
            if (debit == 0 && credit == 0) return;
            mapDict.TryGetValue((role, dept ?? ""), out var m);
            var code = m?.AccountCode;
            var mapped = m != null;
            if (mapped) expectedByAcc[code!] = expectedByAcc.GetValueOrDefault(code!) + (debit - credit);
            else warnings.Add($"ยังไม่ได้แมพบัญชี: {RoleLabels[role]}{(string.IsNullOrEmpty(dept) ? "" : $" ({dept})")}");
            lines.Add(new PayrollPostingLine((int)role, RoleLabels[role], dept, code,
                code != null && accNames.TryGetValue(code, out var n) ? n : null, mapped,
                PayrollCalculator.Round2(debit), PayrollCalculator.Round2(credit), 0, 0));
        }

        foreach (var g in byDept)
        {
            var dept = g.Key;
            var salary = g.Sum(i => i.Salary);
            var allow = g.Sum(i => i.HousingAllowance + i.FoodAllowance + i.Overtime + i.Diligence + i.Bonus + i.OtherIncome);
            // ค่าจ้างรายวัน = ส่วนที่เหลือจากรวมรายได้ (อิงยอดบันทึกจริง ให้ salary+daily+allow = Gross เป๊ะ ดุลพอดี)
            var daily = g.Sum(i => i.GrossIncome - i.Salary - i.HousingAllowance - i.FoodAllowance - i.Overtime - i.Diligence - i.Bonus - i.OtherIncome);
            var employerSso = g.Sum(i => i.SsoEmployee); // 1:1
            var ded = g.Sum(i => i.Absence + i.Advance + i.OtherDeduction);
            var net = g.Sum(i => i.NetPay);

            decimal salaryDr = salary;
            if (Has(PayrollPostingRole.DailyWageExpense, dept)) Add(PayrollPostingRole.DailyWageExpense, dept, daily, 0); else salaryDr += daily;
            if (Has(PayrollPostingRole.AllowanceExpense, dept)) Add(PayrollPostingRole.AllowanceExpense, dept, allow, 0); else salaryDr += allow;
            bool dedMapped = Has(PayrollPostingRole.EmployeeDeductionCredit, dept);
            if (!dedMapped) salaryDr -= ded; // พับหักพนักงานเข้าค่าใช้จ่าย (ลดเดบิต) ให้ดุล
            Add(PayrollPostingRole.SalaryExpense, dept, salaryDr, 0);
            Add(PayrollPostingRole.EmployerSsoExpense, dept, employerSso, 0);
            if (dedMapped) Add(PayrollPostingRole.EmployeeDeductionCredit, dept, 0, ded);
            Add(PayrollPostingRole.NetPayCredit, dept, 0, net);
        }

        var empSsoTotal = run.Items.Sum(i => i.SsoEmployee);
        Add(PayrollPostingRole.SsoPayable, null, 0, empSsoTotal * 2); // ลูกจ้าง+นายจ้าง
        Add(PayrollPostingRole.WhtPayable, null, 0, run.Items.Sum(i => i.WithholdingTax));

        // กระทบยอด: แนบ GL movement ต่อบัญชี (แสดงครั้งเดียวต่อบัญชี)
        var shown = new HashSet<string>();
        for (int i = 0; i < lines.Count; i++)
        {
            var ln = lines[i];
            if (ln.AccountCode is { } code && shown.Add(code))
            {
                var gl = glMove.GetValueOrDefault(code);
                lines[i] = ln with { GlMovement = PayrollCalculator.Round2(gl), Diff = PayrollCalculator.Round2(expectedByAcc[code] - gl) };
            }
        }

        var totalDr = lines.Sum(l => l.Debit);
        var totalCr = lines.Sum(l => l.Credit);
        return new PayrollPostingDto(run.Id, run.Year, run.Month,
            Math.Abs(totalDr - totalCr) < 0.01m, PayrollCalculator.Round2(totalDr), PayrollCalculator.Round2(totalCr),
            lines, warnings.Distinct().ToList());
    }

    private static int rankDept(string name) =>
        System.Text.RegularExpressions.Regex.IsMatch(name, "บริการ|บริหาร|สำนักงาน") ? 0
        : System.Text.RegularExpressions.Regex.IsMatch(name, "ผลิต") ? 1 : 2;
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
