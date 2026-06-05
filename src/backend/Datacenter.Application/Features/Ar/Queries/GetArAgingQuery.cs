using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Ar.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ar.Queries;

/// <summary>รายงานอายุหนี้ลูกหนี้ (aging) ณ วันที่ระบุ (ไม่ระบุ = วันนี้) — แบ่งช่วงตามวันเกินกำหนดจาก DueDate</summary>
public record GetArAgingQuery(int ClientCompanyId, DateTime? AsOf = null)
    : IRequest<ArAgingReportDto>, IRequireCompanyAccess;

public class GetArAgingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetArAgingQuery, ArAgingReportDto>
{
    public async Task<ArAgingReportDto> Handle(GetArAgingQuery request, CancellationToken ct)
    {
        var asOf = (request.AsOf ?? DateTime.Today).Date;

        var clientName = await db.ClientCompanies
            .AsNoTracking().Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.LegalName).FirstOrDefaultAsync(ct) ?? "";

        var open = await db.ArInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId && i.OutstandingAmount > 0)
            .Select(i => new { i.CustomerCode, i.CustomerName, i.DueDate, i.DocumentDate, i.OutstandingAmount })
            .ToListAsync(ct);

        var groups = open
            .GroupBy(i => i.CustomerCode)
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
                var name = g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key;
                return new ArAgingRowDto(g.Key, name!, notDue, d1, d2, d3, d4, notDue + d1 + d2 + d3 + d4);
            })
            .Where(r => r.Total > 0)
            .OrderByDescending(r => r.Total)
            .ToList();

        // ความสดของข้อมูล: เวลานำเข้าใบแจ้งหนี้ลูกหนี้ล่าสุด
        DateTime? dataAsOf = await db.ArInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId)
            .MaxAsync(i => (DateTime?)i.CreatedAt, ct);

        return new ArAgingReportDto(request.ClientCompanyId, clientName, asOf, groups, dataAsOf);
    }
}
