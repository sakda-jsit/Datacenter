using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Ar.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ar.Queries;

/// <summary>รายชื่อลูกค้า + ยอดค้างรวมต่อราย</summary>
public record GetCustomersQuery(int ClientCompanyId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<CustomerDto>>, IRequireCompanyAccess;

public class GetCustomersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public async Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var customers = await db.Customers
            .AsNoTracking()
            .Where(c => c.ClientCompanyId == request.ClientCompanyId && (request.IncludeInactive || c.IsActive))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        // ยอดค้างต่อลูกค้า (sum OutstandingAmount ของใบที่ยังไม่ปิด)
        var outstanding = await db.ArInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId && i.OutstandingAmount > 0)
            .GroupBy(i => i.CustomerCode)
            .Select(g => new { Code = g.Key, Sum = g.Sum(x => x.OutstandingAmount), Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, ct);

        return customers.Select(c =>
        {
            outstanding.TryGetValue(c.CustomerCode, out var o);
            return new CustomerDto(
                c.Id, c.CustomerCode, c.Prefix, c.Name, c.TaxId, c.Address, c.Phone, c.Contact, c.Email,
                c.PaymentTermDays, c.PaymentCondition, c.GlAccountCode, c.IsActive,
                o?.Sum ?? 0m, o?.Count ?? 0);
        }).ToList();
    }
}
