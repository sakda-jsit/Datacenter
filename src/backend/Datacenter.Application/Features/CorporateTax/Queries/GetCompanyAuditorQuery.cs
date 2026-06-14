using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// ผู้ลงนามของ (บริษัท, ปีงบ): ค่าเริ่มต้นบริษัท + override รายปี + ค่าที่ใช้จริง (resolved).
/// </summary>
public record GetCompanySignersQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<CompanySignersDto>, IRequireCompanyAccess;

public class GetCompanySignersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCompanySignersQuery, CompanySignersDto>
{
    public async Task<CompanySignersDto> Handle(GetCompanySignersQuery req, CancellationToken ct)
    {
        var company = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.Id == req.ClientCompanyId)
            .Select(c => new { c.DefaultAuditorId, c.DefaultBookkeeperId })
            .FirstOrDefaultAsync(ct);

        var year = await db.CompanyAuditors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        int? defA = company?.DefaultAuditorId, defB = company?.DefaultBookkeeperId;
        int? yrA = year?.AuditorId, yrB = year?.BookkeeperId;

        return new CompanySignersDto(
            req.ClientCompanyId, req.FiscalYear,
            DefaultAuditorId: defA, DefaultBookkeeperId: defB,
            YearAuditorId: yrA, YearBookkeeperId: yrB,
            ResolvedAuditorId: yrA ?? defA,
            ResolvedBookkeeperId: yrB ?? defB,
            SignDate: year?.SignDate,
            HasYearOverride: yrA is not null || yrB is not null);
    }
}
