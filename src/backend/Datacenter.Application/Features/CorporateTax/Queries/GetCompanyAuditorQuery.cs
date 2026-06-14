using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// ผู้ตรวจสอบและรับรองบัญชีของ (บริษัท, ปีงบ). ถ้ายังไม่เคยบันทึก → คืนค่าว่าง (Exists=false).
/// </summary>
public record GetCompanyAuditorQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<CompanyAuditorDto>, IRequireCompanyAccess;

public class GetCompanyAuditorQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCompanyAuditorQuery, CompanyAuditorDto>
{
    public async Task<CompanyAuditorDto> Handle(GetCompanyAuditorQuery req, CancellationToken ct)
    {
        var e = await db.CompanyAuditors.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientCompanyId == req.ClientCompanyId
                                   && x.FiscalYear == req.FiscalYear, ct);

        return e is null
            ? new CompanyAuditorDto(req.ClientCompanyId, req.FiscalYear, "", null, null, null, null, Exists: false)
            : new CompanyAuditorDto(e.ClientCompanyId, e.FiscalYear, e.AuditorName,
                e.AuditorLicenseNo, e.AuditorTaxId, e.SignDate, e.Note, Exists: true);
    }
}
