using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

/// <summary>
/// ตั้งค่า template งานประจำ. ClientCompanyId = null/0 → ระดับ global; >0 → เฉพาะบริษัท (override).
/// DueDay: null = ใช้ค่าเริ่มต้น; ค่า ≤ 0 = สิ้นเดือนถัดไป.
/// </summary>
public record UpsertComplianceTaskTemplateCommand(
    int? ClientCompanyId, ComplianceTaskType TaskType, bool Enabled, int? DueDay)
    : IRequest<Unit>;

public class UpsertComplianceTaskTemplateCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<UpsertComplianceTaskTemplateCommand, Unit>
{
    public async Task<Unit> Handle(UpsertComplianceTaskTemplateCommand request, CancellationToken ct)
    {
        int? companyId = request.ClientCompanyId is > 0 ? request.ClientCompanyId : null;

        var row = await db.ComplianceTaskTemplates
            .FirstOrDefaultAsync(t => t.ClientCompanyId == companyId && t.TaskType == request.TaskType, ct);

        if (row is null)
        {
            row = new ComplianceTaskTemplate
            {
                ClientCompanyId = companyId,
                TaskType = request.TaskType,
            };
            db.ComplianceTaskTemplates.Add(row);
        }
        row.Enabled = request.Enabled;
        row.DueDay = request.DueDay is > 0 ? request.DueDay : (request.DueDay is null ? null : 0);

        await audit.LogAsync("UpsertTaskTemplate", "ComplianceTaskTemplate",
            entityId: $"{companyId?.ToString() ?? "GLOBAL"}:{request.TaskType}",
            afterValue: $"enabled={request.Enabled} dueDay={request.DueDay}",
            companyId: companyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>ลบ override ระดับบริษัท → กลับไป inherit จาก global/ค่าเริ่มต้น</summary>
public record ResetComplianceTaskTemplateCommand(int ClientCompanyId, ComplianceTaskType TaskType)
    : IRequest<Unit>;

public class ResetComplianceTaskTemplateCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<ResetComplianceTaskTemplateCommand, Unit>
{
    public async Task<Unit> Handle(ResetComplianceTaskTemplateCommand request, CancellationToken ct)
    {
        var row = await db.ComplianceTaskTemplates
            .FirstOrDefaultAsync(t => t.ClientCompanyId == request.ClientCompanyId && t.TaskType == request.TaskType, ct);
        if (row is not null)
        {
            db.ComplianceTaskTemplates.Remove(row);
            await audit.LogAsync("ResetTaskTemplate", "ComplianceTaskTemplate",
                entityId: $"{request.ClientCompanyId}:{request.TaskType}",
                companyId: request.ClientCompanyId, cancellationToken: ct);
            await db.SaveChangesAsync(ct);
        }
        return Unit.Value;
    }
}
