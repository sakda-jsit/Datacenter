using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ReportPackages.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ReportPackages.Queries;

/// <summary>รายการชุดรายงานงบของบริษัท (ทุกปี/version; Year=0 = ทั้งหมด)</summary>
public record GetReportPackagesQuery(int ClientCompanyId, int Year = 0)
    : IRequest<IReadOnlyList<ReportPackageDto>>, IRequireCompanyAccess;

public class GetReportPackagesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReportPackagesQuery, IReadOnlyList<ReportPackageDto>>
{
    public async Task<IReadOnlyList<ReportPackageDto>> Handle(GetReportPackagesQuery request, CancellationToken ct)
    {
        var q = db.ReportPackages.AsNoTracking()
            .Where(p => p.ClientCompanyId == request.ClientCompanyId);
        if (request.Year > 0) q = q.Where(p => p.FiscalYear == request.Year);

        var list = await q
            .OrderByDescending(p => p.FiscalYear).ThenByDescending(p => p.Version)
            .ToListAsync(ct);

        return list.Select(ReportPackageMapper.ToDto).ToList();
    }
}
