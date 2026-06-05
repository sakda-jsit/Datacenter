using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Ap.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ap.Queries;

/// <summary>รายชื่อผู้ขาย + ยอดค้างชำระรวมต่อราย</summary>
public record GetSuppliersQuery(int ClientCompanyId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<SupplierDto>>, IRequireCompanyAccess;

public class GetSuppliersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSuppliersQuery, IReadOnlyList<SupplierDto>>
{
    public async Task<IReadOnlyList<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken ct)
    {
        var suppliers = await db.Suppliers
            .AsNoTracking()
            .Where(s => s.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || s.IsActive))
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        var outstanding = await db.ApInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId && i.OutstandingAmount > 0)
            .GroupBy(i => i.SupplierCode)
            .Select(g => new { Code = g.Key, Sum = g.Sum(x => x.OutstandingAmount), Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, ct);

        return suppliers.Select(s =>
        {
            outstanding.TryGetValue(s.SupplierCode, out var o);
            return new SupplierDto(
                s.Id, s.SupplierCode, s.Prefix, s.Name, s.TaxId, s.Address, s.Phone, s.Contact, s.Email,
                s.PaymentTermDays, s.PaymentCondition, s.GlAccountCode, s.IsActive,
                o?.Sum ?? 0m, o?.Count ?? 0);
        }).ToList();
    }
}
