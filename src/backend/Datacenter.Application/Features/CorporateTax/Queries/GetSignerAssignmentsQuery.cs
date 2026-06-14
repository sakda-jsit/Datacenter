using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CorporateTax.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

/// <summary>
/// ภาพรวมการมอบหมายผู้ลงนาม (ผู้สอบ/ผู้ทำบัญชีประจำ) ของทุกบริษัทที่เข้าถึงได้ — สำหรับจัดการรวมศูนย์.
/// กรองได้ตามชื่อบริษัท / ผู้สอบ / ผู้ทำบัญชี (เช่น "ดูทุกบริษัทของผู้สอบคนนี้").
/// </summary>
public record GetSignerAssignmentsQuery(string? Search, int? AuditorId, int? BookkeeperId)
    : IRequest<IReadOnlyList<SignerAssignmentDto>>;

public class GetSignerAssignmentsQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetSignerAssignmentsQuery, IReadOnlyList<SignerAssignmentDto>>
{
    public async Task<IReadOnlyList<SignerAssignmentDto>> Handle(GetSignerAssignmentsQuery req, CancellationToken ct)
    {
        var q = db.ClientCompanies.AsNoTracking().Where(c => c.IsActive);

        var ids = await accessGuard.GetAccessibleCompanyIdsAsync(ct);
        if (ids is not null)
            q = q.Where(c => ids.Contains(c.Id));

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim().ToLower();
            q = q.Where(c => c.LegalName.ToLower().Contains(s) || c.Name.ToLower().Contains(s) || c.Code.ToLower().Contains(s));
        }
        if (req.AuditorId is { } aid) q = q.Where(c => c.DefaultAuditorId == aid);
        if (req.BookkeeperId is { } bid) q = q.Where(c => c.DefaultBookkeeperId == bid);

        return await q
            .OrderBy(c => c.LegalName)
            .Select(c => new SignerAssignmentDto(
                c.Id,
                c.LegalName == "" ? c.Name : c.LegalName,
                c.Code,
                c.DefaultAuditorId,
                c.DefaultAuditor != null ? c.DefaultAuditor.Name : null,
                c.DefaultBookkeeperId,
                c.DefaultBookkeeper != null ? c.DefaultBookkeeper.Name : null,
                db.CompanyAuditors.Count(x => x.ClientCompanyId == c.Id && (x.AuditorId != null || x.BookkeeperId != null))))
            .ToListAsync(ct);
    }
}
