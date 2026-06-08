using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Application.Features.Payroll.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── ภ.ง.ด.1ก: สรุปเงินได้/ภาษีหัก ณ ที่จ่ายเงินเดือน (มาตรา 40(1)) ทั้งปี ต่อพนักงาน ──
public record GetPnd1kQuery(int ClientCompanyId, int Year) : IRequest<Pnd1kDto>, IRequireCompanyAccess;

public class GetPnd1kQueryHandler(IApplicationDbContext db) : IRequestHandler<GetPnd1kQuery, Pnd1kDto>
{
    public async Task<Pnd1kDto> Handle(GetPnd1kQuery request, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var items = await db.PayrollItems.AsNoTracking()
            .Include(i => i.Employee)
            .Include(i => i.PayrollRun)
            .Where(i => i.PayrollRun!.ClientCompanyId == request.ClientCompanyId && i.PayrollRun.Year == request.Year)
            .ToListAsync(ct);

        // เงินได้ ภ.ง.ด.1ก ต่อรายการ = รวมรายได้ − ขาดงาน (รายได้สุทธิหลังหักลา)
        var rows = items
            .Where(i => i.Employee != null)
            .GroupBy(i => i.EmployeeId)
            .Select(g =>
            {
                var e = g.First().Employee!;
                return new
                {
                    Nat = Digits(e.NationalId), e.Prefix, e.FirstName, e.LastName,
                    Address = Services.EmployeeAddressMapper.ToDto(e),
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

public record GetPnd1kExcelQuery(int ClientCompanyId, int Year) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetPnd1kExcelQueryHandler(ISender sender, IPnd1kExportService svc)
    : IRequestHandler<GetPnd1kExcelQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPnd1kExcelQuery req, CancellationToken ct)
        => svc.BuildExcel(await sender.Send(new GetPnd1kQuery(req.ClientCompanyId, req.Year), ct));
}

public record GetPnd1kPdfQuery(int ClientCompanyId, int Year) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetPnd1kPdfQueryHandler(ISender sender, IPnd1kExportService svc)
    : IRequestHandler<GetPnd1kPdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPnd1kPdfQuery req, CancellationToken ct)
        => svc.BuildPdf(await sender.Send(new GetPnd1kQuery(req.ClientCompanyId, req.Year), ct));
}

// ── TXT (e-Filing กรมสรรพากร) ────────────────────────────────────────────────────
public record GetPnd1kTxtQuery(int ClientCompanyId, int Year) : IRequest<byte[]>, IRequireCompanyAccess;
public class GetPnd1kTxtQueryHandler(ISender sender, IPnd1kExportService svc)
    : IRequestHandler<GetPnd1kTxtQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPnd1kTxtQuery req, CancellationToken ct)
        => svc.BuildTxt(await sender.Send(new GetPnd1kQuery(req.ClientCompanyId, req.Year), ct));
}
