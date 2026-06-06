using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── ดาวน์โหลด Excel template ของงวด (รายชื่อพนักงาน + ค่าปัจจุบัน) ───────────────
public record GetPayrollRunTemplateQuery(int ClientCompanyId, int RunId)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetPayrollRunTemplateQueryHandler(IApplicationDbContext db, IPayrollExcelService excel)
    : IRequestHandler<GetPayrollRunTemplateQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPayrollRunTemplateQuery request, CancellationToken ct)
    {
        var run = await db.PayrollRuns.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);

        var company = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.Name).FirstOrDefaultAsync(ct) ?? "";

        var rows = run.Items.OrderBy(i => i.Employee!.EmployeeCode).Select(i => new PayrollExcelRow(
            i.Employee!.EmployeeCode,
            ((i.Employee.Prefix ?? "") + " " + i.Employee.FirstName + " " + i.Employee.LastName).Trim(),
            i.Salary, i.DailyWageDays, i.DailyWageRate, i.HousingAllowance, i.FoodAllowance, i.Overtime,
            i.Diligence, i.Bonus, i.OtherIncome, i.SsoWageBase, i.SsoEmployee, i.WithholdingTax,
            i.Absence, i.Advance, i.OtherDeduction)).ToList();

        return excel.BuildTemplate(run.Year, run.Month, company, rows);
    }
}

// ── อัปโหลด Excel → แทนที่ค่ารายการในงวด (แก้ไข = อัปโหลดใหม่) ───────────────────
public record ImportPayrollRunCommand(int ClientCompanyId, int RunId, byte[] File)
    : IRequest<int>, IRequireCompanyAccess;   // คืนจำนวนแถวที่อัปเดต

public class ImportPayrollRunCommandHandler(IApplicationDbContext db, IPayrollExcelService excel)
    : IRequestHandler<ImportPayrollRunCommand, int>
{
    public async Task<int> Handle(ImportPayrollRunCommand request, CancellationToken ct)
    {
        var run = await db.PayrollRuns
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.RunId && r.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollRun", request.RunId);

        IReadOnlyList<PayrollExcelRow> parsed;
        try { parsed = excel.Parse(request.File); }
        catch
        {
            throw new Datacenter.Application.Common.Exceptions.ValidationException(new[] {
                new FluentValidation.Results.ValidationFailure("File", "อ่านไฟล์ Excel ไม่ได้ — ใช้ template ที่ดาวน์โหลดจากระบบ") });
        }

        var byCode = run.Items.ToDictionary(i => i.Employee!.EmployeeCode);
        int updated = 0;
        foreach (var row in parsed)
        {
            if (!byCode.TryGetValue(row.EmployeeCode, out var it)) continue;
            it.Salary = row.Salary; it.DailyWageDays = row.DailyWageDays; it.DailyWageRate = row.DailyWageRate;
            it.HousingAllowance = row.HousingAllowance; it.FoodAllowance = row.FoodAllowance; it.Overtime = row.Overtime;
            it.Diligence = row.Diligence; it.Bonus = row.Bonus; it.OtherIncome = row.OtherIncome;
            it.SsoWageBase = row.SsoWageBase; it.SsoEmployee = row.SsoEmployee; it.WithholdingTax = row.WithholdingTax;
            it.Absence = row.Absence; it.Advance = row.Advance; it.OtherDeduction = row.OtherDeduction;
            PayrollCalculator.Recompute(it);
            updated++;
        }

        if (updated == 0)
            throw new Datacenter.Application.Common.Exceptions.ValidationException(new[] {
                new FluentValidation.Results.ValidationFailure("File", "ไม่พบรหัสพนักงานในไฟล์ที่ตรงกับงวดนี้") });

        await db.SaveChangesAsync(ct);
        return updated;
    }
}
