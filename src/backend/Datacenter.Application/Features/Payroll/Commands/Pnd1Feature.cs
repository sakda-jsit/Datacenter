using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── ภ.ง.ด.1 รายเดือน (ใบแนบ): เงินได้/ภาษีหัก ณ ที่จ่ายเงินเดือน ม.40(1) ต่อพนักงาน เฉพาะเดือน ──
// ใช้รูปแบบ/template เดียวกับ ภ.ง.ด.1ก (Pnd1kDto + IPnd1kExportService.BuildTxt) ต่างที่กรองเฉพาะงวดเดือนนั้น
public record GetPnd1Query(int ClientCompanyId, int Year, int Month) : IRequest<Pnd1kDto>, IRequireCompanyAccess;

public class GetPnd1QueryHandler(IApplicationDbContext db) : IRequestHandler<GetPnd1Query, Pnd1kDto>
{
    public async Task<Pnd1kDto> Handle(GetPnd1Query request, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var items = await db.PayrollItems.AsNoTracking()
            .Include(i => i.Employee)
            .Include(i => i.PayrollRun)
            .Where(i => i.PayrollRun!.ClientCompanyId == request.ClientCompanyId
                     && i.PayrollRun.Year == request.Year && i.PayrollRun.Month == request.Month)
            .ToListAsync(ct);

        var rows = items
            .Where(i => i.Employee != null)
            .GroupBy(i => i.EmployeeId)
            .Select(g =>
            {
                var e = g.First().Employee!;
                return new
                {
                    Nat = Digits(e.NationalId), e.Prefix, e.FirstName, e.LastName,
                    Address = EmployeeAddressMapper.ToDto(e),
                    Income = PayrollCalculator.Round2(g.Sum(i => i.GrossIncome - i.Absence)),
                    Tax = PayrollCalculator.Round2(g.Sum(i => i.WithholdingTax)),
                };
            })
            .Where(x => x.Income > 0)
            .OrderBy(x => x.Nat, StringComparer.Ordinal)
            .Select((x, idx) => new Pnd1kRow(
                idx + 1, x.Nat, x.Prefix ?? "", x.FirstName, x.LastName,
                "40(1)", x.Income, x.Tax, 1, x.Address)) // 1 = หัก ณ ที่จ่าย
            .ToList();

        return new Pnd1kDto(
            request.Year,
            string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            company.TaxId, company.Address,
            rows, rows.Sum(r => r.AnnualIncome), rows.Sum(r => r.AnnualTax), rows.Count);
    }

    private static string Digits(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", @"\D", "");
}

// ── TXT (e-Filing) — ใช้ BuildTxt ตัวเดียวกับ ภ.ง.ด.1ก ──
public record GetPnd1TxtQuery(int ClientCompanyId, int Year, int Month) : IRequest<byte[]>, IRequireCompanyAccess;

public class GetPnd1TxtQueryHandler(ISender sender, IPnd1kExportService svc)
    : IRequestHandler<GetPnd1TxtQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPnd1TxtQuery req, CancellationToken ct)
        => svc.BuildTxt(await sender.Send(new GetPnd1Query(req.ClientCompanyId, req.Year, req.Month), ct));
}
