using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Ap.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ap.Queries;

/// <summary>ใบตั้งหนี้เจ้าหนี้ (ตัวกรอง: ปี, เฉพาะค้างชำระ, รหัสผู้ขาย)</summary>
public record GetApInvoicesQuery(int ClientCompanyId, int Year = 0, bool OutstandingOnly = false, string? SupplierCode = null)
    : IRequest<IReadOnlyList<ApInvoiceDto>>, IRequireCompanyAccess;

public class GetApInvoicesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApInvoicesQuery, IReadOnlyList<ApInvoiceDto>>
{
    public async Task<IReadOnlyList<ApInvoiceDto>> Handle(GetApInvoicesQuery request, CancellationToken ct)
    {
        var q = db.ApInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId);

        if (request.Year > 0) q = q.Where(i => i.DocumentDate.Year == request.Year);
        if (request.OutstandingOnly) q = q.Where(i => i.OutstandingAmount > 0);
        if (!string.IsNullOrWhiteSpace(request.SupplierCode)) q = q.Where(i => i.SupplierCode == request.SupplierCode);

        return await q
            .OrderBy(i => i.DocumentDate).ThenBy(i => i.DocumentNo)
            .Select(i => new ApInvoiceDto(
                i.Id, i.DocumentNo, i.DocumentDate, i.DueDate, i.SupplierCode, i.SupplierName,
                i.Amount, i.VatAmount, i.NetAmount, i.PaidAmount, i.OutstandingAmount, i.IsCompleted, i.Reference))
            .ToListAsync(ct);
    }
}
