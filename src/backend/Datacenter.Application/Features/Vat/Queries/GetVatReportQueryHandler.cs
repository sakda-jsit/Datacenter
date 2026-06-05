using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Vat.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Queries;

public class GetVatReportQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVatReportQuery, VatReportDto>
{
    public async Task<VatReportDto> Handle(GetVatReportQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.LegalName)
            .FirstOrDefaultAsync(ct) ?? "";

        // สรุปต่อ (เดือน, ประเภท) ในฐานข้อมูล
        var grouped = await db.VatEntries
            .AsNoTracking()
            .Where(v => v.ClientCompanyId == request.ClientCompanyId && v.TaxPeriod.Year == request.Year)
            .GroupBy(v => new { v.TaxPeriod.Month, v.VatType })
            .Select(g => new
            {
                g.Key.Month,
                g.Key.VatType,
                Base = g.Sum(x => x.BaseAmount),
                Vat = g.Sum(x => x.VatAmount),
                Zero = g.Sum(x => x.ZeroRatedAmount),
                Count = g.Count(),
            })
            .ToListAsync(ct);

        var months = new List<VatMonthlyDto>(12);
        for (int m = 1; m <= 12; m++)
        {
            var output = grouped.FirstOrDefault(x => x.Month == m && x.VatType == VatEntryType.Output);
            var input = grouped.FirstOrDefault(x => x.Month == m && x.VatType == VatEntryType.Input);

            months.Add(new VatMonthlyDto(
                Month: m,
                OutputBase: Math.Round(output?.Base ?? 0m, 2),
                OutputVat: Math.Round(output?.Vat ?? 0m, 2),
                OutputZeroRated: Math.Round(output?.Zero ?? 0m, 2),
                OutputCount: output?.Count ?? 0,
                InputBase: Math.Round(input?.Base ?? 0m, 2),
                InputVat: Math.Round(input?.Vat ?? 0m, 2),
                InputCount: input?.Count ?? 0));
        }

        return new VatReportDto(request.ClientCompanyId, client, request.Year, months);
    }
}
