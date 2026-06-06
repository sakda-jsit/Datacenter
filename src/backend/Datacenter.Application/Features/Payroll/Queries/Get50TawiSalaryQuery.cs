using Datacenter.Application.Common;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Queries;

// ── หนังสือรับรองการหักภาษี ณ ที่จ่าย (50 ทวิ) เงินเดือน — มาตรา 40(1) ทั้งปี ต่อพนักงาน ──
// ใช้เลย์เอาต์เดียวกับ 50 ทวิ ของ WHT (IWhtCertificatePdfService) แต่:
//   • แบบยื่น = ภ.ง.ด.1ก, ประเภทเงินได้ = ข้อ 1 (เงินเดือน), เงื่อนไข = หักภาษี ณ ที่จ่าย
//   • แสดงบล็อกท้ายฟอร์ม "เงินสะสมเข้ากองทุนประกันสังคม" (= ปกส.สมทบของลูกจ้างทั้งปี)
// EmployeeIds ว่าง = ออกให้ทุกคนที่มีเงินได้ในปีนั้น
public record Get50TawiSalaryPdfQuery(int ClientCompanyId, int Year, IReadOnlyList<int>? EmployeeIds = null)
    : IRequest<byte[]>, IRequireCompanyAccess;

public record Get50TawiSalaryImagesQuery(int ClientCompanyId, int Year, IReadOnlyList<int>? EmployeeIds = null)
    : IRequest<IReadOnlyList<string>>, IRequireCompanyAccess;

public static class SalaryCertificateBuilder
{
    /// <summary>รวม PayrollItems ทั้งปีต่อพนักงาน → WhtCertificateModel (เงินเดือน ม.40(1))</summary>
    public static async Task<List<WhtCertificateModel>> BuildAsync(
        IApplicationDbContext db, int clientCompanyId, int year, IReadOnlyList<int>? employeeIds, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", clientCompanyId);

        var ids = employeeIds?.Where(i => i > 0).Distinct().ToList();

        var query = db.PayrollItems.AsNoTracking()
            .Include(i => i.Employee)
            .Include(i => i.PayrollRun)
            .Where(i => i.PayrollRun!.ClientCompanyId == clientCompanyId && i.PayrollRun.Year == year);
        if (ids is { Count: > 0 })
            query = query.Where(i => ids.Contains(i.EmployeeId));
        var items = await query.ToListAsync(ct);

        var payerName = string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName;
        var issueDate = new DateTime(year, 12, 31); // หนังสือรับรองรายปี — ออก ณ สิ้นปี

        var models = items
            .Where(i => i.Employee != null)
            .GroupBy(i => i.EmployeeId)
            .Select(g =>
            {
                var e = g.First().Employee!;
                var income = PayrollCalculator.Round2(g.Sum(i => i.GrossIncome - i.Absence));
                var tax = PayrollCalculator.Round2(g.Sum(i => i.WithholdingTax));
                var sso = PayrollCalculator.Round2(g.Sum(i => i.SsoEmployee));
                var name = string.IsNullOrWhiteSpace(e.Prefix) ? $"{e.FirstName} {e.LastName}".Trim()
                                                               : $"{e.Prefix}{e.FirstName} {e.LastName}".Trim();
                return new
                {
                    Nat = Digits(e.NationalId), Name = name, e.Address, Income = income, Tax = tax, Sso = sso,
                };
            })
            .Where(x => x.Income > 0)
            .OrderBy(x => x.Nat, StringComparer.Ordinal)
            .Select((x, idx) => new WhtCertificateModel(
                FormLabel:      "ภ.ง.ด.1ก",
                SequenceNo:     $"{(year + 543) % 100:00}-{idx + 1:0000}",
                PayerName:      payerName,
                PayerTaxId:     company.TaxId ?? "",
                PayerAddress:   company.Address,
                PayeeName:      x.Name,
                PayeeTaxId:     x.Nat,
                PayeeAddress:   x.Address,
                IncomeType:     "เงินเดือน",
                PayDate:        issueDate,
                Amount:         x.Income,
                TaxAmount:      x.Tax,
                TaxRate:        0m,
                AmountInWords:  ThaiBahtText.Convert(x.Tax),
                IssueDate:      issueDate,
                IncomeCategory: 1,   // 1 = เงินเดือน ม.40(1)
                ConditionType:  1,   // หักภาษี ณ ที่จ่าย
                PayerBranchCode: company.BranchCode,
                PayerSignature:  company.SignatureImage,
                SsoContribution: x.Sso))
            .ToList();

        if (models.Count == 0)
            throw new NotFoundException("PayrollItem", $"{clientCompanyId}/{year}");

        return models;
    }

    private static string Digits(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\D", "");
}

public class Get50TawiSalaryPdfQueryHandler(IApplicationDbContext db, IWhtCertificatePdfService pdf)
    : IRequestHandler<Get50TawiSalaryPdfQuery, byte[]>
{
    public async Task<byte[]> Handle(Get50TawiSalaryPdfQuery req, CancellationToken ct)
        => pdf.Generate(await SalaryCertificateBuilder.BuildAsync(db, req.ClientCompanyId, req.Year, req.EmployeeIds, ct));
}

public class Get50TawiSalaryImagesQueryHandler(IApplicationDbContext db, IWhtCertificatePdfService pdf)
    : IRequestHandler<Get50TawiSalaryImagesQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(Get50TawiSalaryImagesQuery req, CancellationToken ct)
        => pdf.GenerateImages(await SalaryCertificateBuilder.BuildAsync(db, req.ClientCompanyId, req.Year, req.EmployeeIds, ct))
            .Select(png => "data:image/png;base64," + System.Convert.ToBase64String(png))
            .ToList();
}
