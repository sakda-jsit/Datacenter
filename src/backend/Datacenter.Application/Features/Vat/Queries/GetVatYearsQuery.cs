using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Queries;

/// <summary>ปีภาษี (จาก TaxPeriod) ที่มีข้อมูล VAT ของบริษัท — ใช้เติมตัวเลือกปีในหน้า ภ.พ.30</summary>
public record GetVatYearsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<int>>, IRequireCompanyAccess;

public class GetVatYearsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetVatYearsQuery, IReadOnlyList<int>>
{
    public async Task<IReadOnlyList<int>> Handle(GetVatYearsQuery request, CancellationToken ct)
    {
        return await db.VatEntries
            .AsNoTracking()
            .Where(v => v.ClientCompanyId == request.ClientCompanyId)
            .Select(v => v.TaxPeriod.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
    }
}
