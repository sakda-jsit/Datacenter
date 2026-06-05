using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

/// <summary>
/// บันทึก/แก้ไขข้อความ NOTE2 เป็น "เฉพาะบริษัท" (override default กลาง) สำหรับปีที่มีผลที่ระบุ.
/// ไม่แตะ template กลาง (ClientCompanyId = null) เพื่อกันข้อความมาตรฐานหาย.
/// </summary>
public record UpsertNoteTemplateSectionCommand(
    int ClientCompanyId,
    int EffectiveYear,
    string NoteKey,
    string Title,
    string BodyText,
    int SortOrder) : IRequest<int>, IRequireCompanyAccess;

public class UpsertNoteTemplateSectionCommandHandler(
    IApplicationDbContext db, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<UpsertNoteTemplateSectionCommand, int>
{
    public async Task<int> Handle(UpsertNoteTemplateSectionCommand request, CancellationToken ct)
    {
        var existing = await db.NoteTemplateSections.FirstOrDefaultAsync(s =>
            s.ClientCompanyId == request.ClientCompanyId &&
            s.EffectiveYear == request.EffectiveYear &&
            s.NoteKey == request.NoteKey, ct);

        string action;
        if (existing is null)
        {
            existing = new NoteTemplateSection
            {
                ClientCompanyId = request.ClientCompanyId,
                EffectiveYear   = request.EffectiveYear,
                NoteKey         = request.NoteKey,
                Title           = request.Title,
                BodyText        = request.BodyText,
                SortOrder       = request.SortOrder,
                CreatedAt       = DateTime.UtcNow,
                CreatedBy       = user.Username,
            };
            db.NoteTemplateSections.Add(existing);
            action = "Create";
        }
        else
        {
            existing.Title      = request.Title;
            existing.BodyText   = request.BodyText;
            existing.SortOrder  = request.SortOrder;
            existing.ModifiedAt = DateTime.UtcNow;
            existing.ModifiedBy = user.Username;
            action = "Update";
        }

        await audit.LogAsync(action, "NoteTemplateSection",
            $"{request.ClientCompanyId}:{request.EffectiveYear}:{request.NoteKey}",
            afterValue: request.Title,
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return existing.Id;
    }
}
