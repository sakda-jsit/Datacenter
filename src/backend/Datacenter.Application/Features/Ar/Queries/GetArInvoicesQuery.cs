using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Ar.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ar.Queries;

/// <summary>
/// ใบแจ้งหนี้ลูกหนี้ (ตัวกรอง: ปี, เฉพาะค้างชำระ, รหัสลูกค้า).
/// Year = 0 → ทุกปี; OutstandingOnly → เฉพาะที่ยังค้าง; CustomerCode = null → ทุกราย.
/// </summary>
public record GetArInvoicesQuery(int ClientCompanyId, int Year = 0, bool OutstandingOnly = false, string? CustomerCode = null)
    : IRequest<IReadOnlyList<ArInvoiceDto>>, IRequireCompanyAccess;

public class GetArInvoicesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetArInvoicesQuery, IReadOnlyList<ArInvoiceDto>>
{
    public async Task<IReadOnlyList<ArInvoiceDto>> Handle(GetArInvoicesQuery request, CancellationToken ct)
    {
        var q = db.ArInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId);

        if (request.Year > 0) q = q.Where(i => i.DocumentDate.Year == request.Year);
        if (request.OutstandingOnly) q = q.Where(i => i.OutstandingAmount > 0);
        if (!string.IsNullOrWhiteSpace(request.CustomerCode)) q = q.Where(i => i.CustomerCode == request.CustomerCode);

        return await q
            .OrderBy(i => i.DocumentDate).ThenBy(i => i.DocumentNo)
            .Select(i => new ArInvoiceDto(
                i.Id, i.DocumentNo, i.DocumentDate, i.DueDate, i.CustomerCode, i.CustomerName,
                i.Amount, i.VatAmount, i.NetAmount, i.ReceivedAmount, i.OutstandingAmount, i.IsCompleted, i.Reference))
            .ToListAsync(ct);
    }
}
