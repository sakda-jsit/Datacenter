using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// รายการข้อความ template ของ NOTE2 ที่ "มีผล" สำหรับบริษัท/ปีงบ (สำหรับหน้าจอแก้ไข):
/// ต่อ NoteKey เลือกบริษัท override ก่อน default กลาง, EffectiveYear มากสุดที่ ≤ ปีงบ.
/// </summary>
public record GetNoteTemplateSectionsQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<IReadOnlyList<NoteTemplateSectionDto>>, IRequireCompanyAccess;

public class GetNoteTemplateSectionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetNoteTemplateSectionsQuery, IReadOnlyList<NoteTemplateSectionDto>>
{
    public async Task<IReadOnlyList<NoteTemplateSectionDto>> Handle(
        GetNoteTemplateSectionsQuery request, CancellationToken ct)
    {
        var candidates = await db.NoteTemplateSections.AsNoTracking()
            .Where(s => (s.ClientCompanyId == null || s.ClientCompanyId == request.ClientCompanyId)
                     && s.EffectiveYear <= request.FiscalYear)
            .ToListAsync(ct);

        return candidates
            .GroupBy(s => s.NoteKey)
            .Select(g => g
                .OrderByDescending(s => s.ClientCompanyId.HasValue)
                .ThenByDescending(s => s.EffectiveYear)
                .First())
            .OrderBy(s => s.SortOrder)
            .Select(s => new NoteTemplateSectionDto(
                s.Id, s.ClientCompanyId, s.EffectiveYear, s.NoteKey, s.Title, s.BodyText, s.SortOrder))
            .ToList();
    }
}
