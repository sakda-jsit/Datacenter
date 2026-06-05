using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Ap.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ap.Queries;

/// <summary>รายงานอายุหนี้เจ้าหนี้ (aging) ณ วันที่ระบุ (ไม่ระบุ = วันนี้)</summary>
public record GetApAgingQuery(int ClientCompanyId, DateTime? AsOf = null)
    : IRequest<ApAgingReportDto>, IRequireCompanyAccess;

public class GetApAgingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApAgingQuery, ApAgingReportDto>
{
    public async Task<ApAgingReportDto> Handle(GetApAgingQuery request, CancellationToken ct)
    {
        var asOf = (request.AsOf ?? DateTime.Today).Date;

        var clientName = await db.ClientCompanies
            .AsNoTracking().Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.LegalName).FirstOrDefaultAsync(ct) ?? "";

        var open = await db.ApInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId && i.OutstandingAmount > 0)
            .Select(i => new { i.SupplierCode, i.SupplierName, i.DueDate, i.DocumentDate, i.OutstandingAmount })
            .ToListAsync(ct);

        var groups = open
            .GroupBy(i => i.SupplierCode)
            .Select(g =>
            {
                decimal notDue = 0, d1 = 0, d2 = 0, d3 = 0, d4 = 0;
                foreach (var i in g)
                {
                    var due = (i.DueDate ?? i.DocumentDate).Date;
                    int overdue = (asOf - due).Days;
                    var amt = i.OutstandingAmount;
                    if (overdue <= 0) notDue += amt;
                    else if (overdue <= 30) d1 += amt;
                    else if (overdue <= 60) d2 += amt;
                    else if (overdue <= 90) d3 += amt;
                    else d4 += amt;
                }
                var name = g.Select(x => x.SupplierName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key;
                return new ApAgingRowDto(g.Key, name!, notDue, d1, d2, d3, d4, notDue + d1 + d2 + d3 + d4);
            })
            .Where(r => r.Total > 0)
            .OrderByDescending(r => r.Total)
            .ToList();

        // ความสดของข้อมูล: เวลานำเข้าใบตั้งหนี้เจ้าหนี้ล่าสุด
        DateTime? dataAsOf = await db.ApInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId)
            .MaxAsync(i => (DateTime?)i.CreatedAt, ct);

        return new ApAgingReportDto(request.ClientCompanyId, clientName, asOf, groups, dataAsOf);
    }
}
