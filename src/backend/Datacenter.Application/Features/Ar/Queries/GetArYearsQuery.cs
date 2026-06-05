using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ar.Queries;

/// <summary>ปีของใบแจ้งหนี้ลูกหนี้ที่มีข้อมูล</summary>
public record GetArYearsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<int>>, IRequireCompanyAccess;

public class GetArYearsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetArYearsQuery, IReadOnlyList<int>>
{
    public async Task<IReadOnlyList<int>> Handle(GetArYearsQuery request, CancellationToken ct)
        => await db.ArInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId)
            .Select(i => i.DocumentDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
}
