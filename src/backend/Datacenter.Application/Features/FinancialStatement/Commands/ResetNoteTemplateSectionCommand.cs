using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

/// <summary>
/// ลบข้อความ NOTE2 เฉพาะบริษัท (override) ของ NoteKey/ปีที่ระบุ → กลับไปใช้ template กลาง (default).
/// ไม่กระทบ template กลาง.
/// </summary>
public record ResetNoteTemplateSectionCommand(
    int ClientCompanyId, int EffectiveYear, string NoteKey) : IRequest<bool>, IRequireCompanyAccess;

public class ResetNoteTemplateSectionCommandHandler(
    IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<ResetNoteTemplateSectionCommand, bool>
{
    public async Task<bool> Handle(ResetNoteTemplateSectionCommand request, CancellationToken ct)
    {
        var existing = await db.NoteTemplateSections.FirstOrDefaultAsync(s =>
            s.ClientCompanyId == request.ClientCompanyId &&
            s.EffectiveYear == request.EffectiveYear &&
            s.NoteKey == request.NoteKey, ct);

        if (existing is null) return false;

        db.NoteTemplateSections.Remove(existing);
        await audit.LogAsync("Reset", "NoteTemplateSection",
            $"{request.ClientCompanyId}:{request.EffectiveYear}:{request.NoteKey}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
