using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>
/// รายงานภาษีขาย (VatType=1) / รายงานภาษีซื้อ (VatType=2) เป็น Excel — ทั้งปีหรือเฉพาะเดือน.
/// </summary>
public record GetVatTaxReportExcelQuery(int ClientCompanyId, int Year, int VatType, int Month = 0)
    : IRequest<byte[]>, IRequireCompanyAccess;

public class GetVatTaxReportExcelQueryHandler(
    IApplicationDbContext db, ISender sender, IVatTaxReportExportService svc)
    : IRequestHandler<GetVatTaxReportExcelQuery, byte[]>
{
    public async Task<byte[]> Handle(GetVatTaxReportExcelQuery req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", req.ClientCompanyId);

        var entries = await sender.Send(
            new GetVatEntriesQuery(req.ClientCompanyId, req.Year, req.Month, req.VatType), ct);

        var ordered = entries
            .OrderBy(e => e.DocumentDate ?? e.TaxPeriod)
            .ThenBy(e => e.DocumentNo, StringComparer.Ordinal)
            .ToList();

        var rows = new List<VatTaxReportRow>(ordered.Count);
        int seq = 1;
        foreach (var e in ordered)
        {
            var name = string.IsNullOrWhiteSpace(e.Description) ? e.ReferenceNo : e.Description;
            rows.Add(new VatTaxReportRow(
                seq++, e.DocumentDate, e.DocumentNo, name, e.CounterpartyTaxId,
                e.BaseAmount, e.VatAmount, e.ZeroRatedAmount));
        }

        var dto = new VatTaxReportDto(
            string.IsNullOrWhiteSpace(company.LegalName) ? company.Name : company.LegalName,
            company.TaxId, req.Year, req.Month, req.VatType,
            rows,
            rows.Sum(x => x.BaseAmount), rows.Sum(x => x.VatAmount), rows.Sum(x => x.ZeroRatedAmount));

        return svc.BuildExcel(dto);
    }
}
