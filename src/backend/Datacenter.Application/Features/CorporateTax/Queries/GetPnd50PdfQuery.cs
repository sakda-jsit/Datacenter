using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// สร้างแบบ ภ.ง.ด.50 (PDF) ของ (บริษัท, ปีงบ) — หัว + การคำนวณภาษีจาก TAX engine (เฟส A).
/// </summary>
public record GetPnd50PdfQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetPnd50PdfQueryHandler(IApplicationDbContext db, ISender sender, IPnd50PdfService svc)
    : IRequestHandler<GetPnd50PdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPnd50PdfQuery req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);

        var startMonth = company.FiscalYearStartMonth is >= 1 and <= 12 ? company.FiscalYearStartMonth : 1;
        var periodStart = new DateTime(req.FiscalYear, startMonth, 1);
        var periodEnd = periodStart.AddYears(1).AddDays(-1);

        var tax = await sender.Send(new GetTaxComputationQuery(req.ClientCompanyId, req.FiscalYear), ct);
        var r = tax.Result;

        var data = new Pnd50FormData(
            CompanyName: string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            TaxId: company.TaxId,
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            NetTaxableIncome: r.NetTaxableIncome,
            TaxAmount: r.TaxAmount,
            WhtCredit: r.WhtCredit,
            TotalCredit: r.WhtCredit,
            NetPayable: r.NetPayable);

        return svc.Build(data);
    }
}
