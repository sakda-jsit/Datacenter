using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Wht.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Queries;

public class GetWhtReportQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWhtReportQuery, WhtReportDto>
{
    public async Task<WhtReportDto> Handle(GetWhtReportQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.LegalName)
            .FirstOrDefaultAsync(ct) ?? "";

        var grouped = await db.WhtEntries
            .AsNoTracking()
            .Where(w => w.ClientCompanyId == request.ClientCompanyId && w.TaxPeriod.Year == request.Year)
            .GroupBy(w => new { w.TaxPeriod.Month, w.FormType })
            .Select(g => new
            {
                g.Key.Month,
                g.Key.FormType,
                Base = g.Sum(x => x.BaseAmount),
                Tax = g.Sum(x => x.TaxAmount),
                Count = g.Count(),
            })
            .ToListAsync(ct);

        var months = new List<WhtMonthlyDto>(12);
        for (int m = 1; m <= 12; m++)
        {
            var p3 = grouped.FirstOrDefault(x => x.Month == m && x.FormType == WhtFormType.Pnd3);
            var p53 = grouped.FirstOrDefault(x => x.Month == m && x.FormType == WhtFormType.Pnd53);

            months.Add(new WhtMonthlyDto(
                Month: m,
                Pnd3Base: Math.Round(p3?.Base ?? 0m, 2),
                Pnd3Tax: Math.Round(p3?.Tax ?? 0m, 2),
                Pnd3Count: p3?.Count ?? 0,
                Pnd53Base: Math.Round(p53?.Base ?? 0m, 2),
                Pnd53Tax: Math.Round(p53?.Tax ?? 0m, 2),
                Pnd53Count: p53?.Count ?? 0));
        }

        // ความสดของข้อมูล: เวลานำเข้า WHT ล่าสุด (ทั้งบริษัท — ไม่จำกัดปี)
        DateTime? dataAsOf = await db.WhtEntries
            .AsNoTracking()
            .Where(w => w.ClientCompanyId == request.ClientCompanyId)
            .MaxAsync(w => (DateTime?)w.CreatedAt, ct);

        return new WhtReportDto(request.ClientCompanyId, client, request.Year, months, dataAsOf);
    }
}
