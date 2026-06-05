using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Wht.Queries;

/// <summary>ปีภาษี (จาก TaxPeriod) ที่มีข้อมูลภาษีหัก ณ ที่จ่ายของบริษัท</summary>
public record GetWhtYearsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<int>>, IRequireCompanyAccess;

public class GetWhtYearsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWhtYearsQuery, IReadOnlyList<int>>
{
    public async Task<IReadOnlyList<int>> Handle(GetWhtYearsQuery request, CancellationToken ct)
    {
        return await db.WhtEntries
            .AsNoTracking()
            .Where(w => w.ClientCompanyId == request.ClientCompanyId)
            .Select(w => w.TaxPeriod.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
    }
}
