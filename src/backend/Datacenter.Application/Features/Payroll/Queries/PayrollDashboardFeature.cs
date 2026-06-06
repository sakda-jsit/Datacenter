using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.Commands;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Queries;

// ── P6: แดชบอร์ด/Checklist + กระทบยอด 3 ทาง (รวม P3/P4/P5) ──────────────────────
// คำนวณจากข้อมูลที่มี (ไม่เพิ่ม entity): สถานะงวด + slip↔ระบบ + เงินเดือน↔GL + ภาษี/ปกส.↔แบบรายปี
public record GetPayrollDashboardQuery(int ClientCompanyId, int Year)
    : IRequest<PayrollDashboardDto>, IRequireCompanyAccess;

public class GetPayrollDashboardQueryHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetPayrollDashboardQuery, PayrollDashboardDto>
{
    public async Task<PayrollDashboardDto> Handle(GetPayrollDashboardQuery request, CancellationToken ct)
    {
        _ = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var runs = await db.PayrollRuns.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Employee)
            .Where(r => r.ClientCompanyId == request.ClientCompanyId && r.Year == request.Year)
            .ToListAsync(ct);

        var rates = await db.PayrollRateConfigs.AsNoTracking().ToListAsync(ct);

        var filings = await db.SsoMonthlyFilings.AsNoTracking()
            .Where(f => f.ClientCompanyId == request.ClientCompanyId && f.Year == request.Year)
            .ToListAsync(ct);

        var months = new List<PayrollChecklistMonth>();
        for (int m = 1; m <= 12; m++)
        {
            var run = runs.FirstOrDefault(r => r.Month == m);
            if (run is null)
            {
                months.Add(new PayrollChecklistMonth(m, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, 0,
                    false, false, false, false, false, false, false));
                continue;
            }

            var cfg = PayrollRates.ResolveEffective(rates, new DateTime(request.Year, m, 1));
            var items = run.Items;
            var gross = PayrollCalculator.Round2(items.Sum(i => i.GrossIncome));
            var net = PayrollCalculator.Round2(items.Sum(i => i.NetPay));
            var ssoEmp = PayrollCalculator.Round2(items.Sum(i => i.SsoEmployee));
            var ssoEr = PayrollCalculator.Round2(items.Sum(i => i.SsoEmployee)); // นายจ้างสมทบเท่าลูกจ้าง
            var tax = PayrollCalculator.Round2(items.Sum(i => i.WithholdingTax));
            var ssoDiff = PayrollCalculator.Round2(
                items.Sum(i => i.SsoEmployee - PayrollCalculator.SsoEmployee(i, cfg)));

            // กระทบยอดเงินเดือน ↔ GL (reuse ใบสำคัญ P4)
            bool balanced = false; decimal glDiff = 0;
            try
            {
                var posting = await sender.Send(new GetPayrollPostingQuery(request.ClientCompanyId, run.Id), ct);
                balanced = posting.Balanced;
                glDiff = PayrollCalculator.Round2(posting.Lines.Sum(l => Math.Abs(l.Diff)));
            }
            catch { /* ไม่มีแมพบัญชี/ลงบัญชีไม่ได้ → ปล่อยเป็นยังไม่กระทบยอด */ }

            var grand = PayrollCalculator.Round2(ssoEmp + ssoEr);
            var filing = filings.FirstOrDefault(f => f.PayrollRunId == run.Id);
            bool ssoFiled = filing?.SubmittedDate != null;
            bool ssoReceipt = filing is { Status: SsoFilingStatus.ReceiptReceived };
            bool receiptMatch = filing?.ReceiptAmount is { } ra && Math.Abs(ra - grand) < 0.05m;

            months.Add(new PayrollChecklistMonth(
                m, true, (int)run.Status, items.Count, gross, net,
                ssoEmp, ssoEr, grand, tax,
                ssoDiff, balanced, glDiff,
                ssoFiled, ssoReceipt, receiptMatch,
                StepRecorded: (int)run.Status >= 1,
                StepBalanced: balanced,
                StepSsoReady: ssoEmp > 0,
                StepHasTax: tax > 0));
        }

        // กระทบยอดรายปี: ภ.ง.ด.1ก + กท.20ก
        decimal pnd1kTax = 0; int pnd1kCount = 0;
        decimal kt20Wage = 0; int kt20Count = 0; decimal kt20Contrib = 0;
        try
        {
            var pnd1k = await sender.Send(new GetPnd1kQuery(request.ClientCompanyId, request.Year), ct);
            pnd1kTax = pnd1k.TotalTax; pnd1kCount = pnd1k.PersonCount;
        }
        catch { }
        try
        {
            var kt20 = await sender.Send(new GetKt20Query(request.ClientCompanyId, request.Year), ct);
            kt20Wage = kt20.TotalWage; kt20Count = kt20.EmployeeCount; kt20Contrib = kt20.Contribution;
        }
        catch { }

        var yearTax = PayrollCalculator.Round2(months.Sum(x => x.Tax));
        var taxDiff = PayrollCalculator.Round2(yearTax - pnd1kTax);

        return new PayrollDashboardDto(
            request.Year,
            months.Count(x => x.HasRun),
            months,
            PayrollCalculator.Round2(months.Sum(x => x.TotalGross)),
            yearTax,
            PayrollCalculator.Round2(months.Sum(x => x.SsoTotal)),
            pnd1kTax, pnd1kCount,
            kt20Wage, kt20Count, kt20Contrib,
            Math.Abs(taxDiff) < 0.05m, taxDiff);
    }
}
