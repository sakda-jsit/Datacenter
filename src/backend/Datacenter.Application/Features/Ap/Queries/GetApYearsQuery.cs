using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Ap.Queries;

/// <summary>ปีของใบตั้งหนี้เจ้าหนี้ที่มีข้อมูล</summary>
public record GetApYearsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<int>>, IRequireCompanyAccess;

public class GetApYearsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApYearsQuery, IReadOnlyList<int>>
{
    public async Task<IReadOnlyList<int>> Handle(GetApYearsQuery request, CancellationToken ct)
        => await db.ApInvoices
            .AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId)
            .Select(i => i.DocumentDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
}
