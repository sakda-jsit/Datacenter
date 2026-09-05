using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class UpdateTaskStatusCommandHandler(
    IApplicationDbContext db,
    IAuditService audit,
    ICompanyAccessGuard accessGuard,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateTaskStatusCommand>
{
    public async Task Handle(UpdateTaskStatusCommand request, CancellationToken ct)
    {
        var task = await db.ComplianceTasks.FindAsync([request.TaskId], ct)
            ?? throw new NotFoundException("ComplianceTask", request.TaskId);

        // task อ้างถึงบริษัทผ่าน TaskId จึงตรวจสิทธิ์หลังโหลด entity แทน pipeline behaviour
        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

        // ปิดงานเป็น "เสร็จสิ้น" ได้ต่อเมื่อมีหลักฐานแนบ (ถ้าประเภทงานนั้นบังคับไว้)
        if (request.Status == ComplianceTaskStatus.Completed && task.Status != ComplianceTaskStatus.Completed)
            await EnsureEvidenceAsync(task, ct);

        var previousStatus = task.Status;

        task.Status = request.Status;
        if (request.Note is not null)
            task.Note = request.Note;

        if (request.Status == ComplianceTaskStatus.Completed)
        {
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedByUserId = currentUser.UserId;
        }
        else
        {
            task.CompletedAt = null;
            task.CompletedByUserId = null;
        }

        await audit.LogAsync(
            action: "UpdateStatus",
            entityName: "ComplianceTask",
            entityId: task.Id.ToString(),
            beforeValue: previousStatus.ToString(),
            afterValue: request.Status.ToString(),
            companyId: task.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// งานยื่นแบบต้องมีหลักฐาน (แบบที่ยื่น/ใบเสร็จ) แนบกับงานงวดนั้นก่อนจึงปิดเป็น "เสร็จสิ้น" ได้.
    /// เปิด/ปิดกฎรายประเภทงานได้ที่ "ตั้งค่างานประจำ" — ค่าเริ่มต้น: งานยื่นแบบ = บังคับ, ปิดบัญชี = ไม่บังคับ
    /// </summary>
    private async Task EnsureEvidenceAsync(Domain.Entities.ComplianceTask task, CancellationToken ct)
    {
        var globalRules = await db.ComplianceTaskTemplates
            .Where(t => t.ClientCompanyId == null && t.TaskType == task.TaskType).ToListAsync(ct);
        var companyRules = await db.ComplianceTaskTemplates
            .Where(t => t.ClientCompanyId == task.ClientCompanyId && t.TaskType == task.TaskType).ToListAsync(ct);

        var effective = ComplianceTemplateResolver.Resolve(globalRules, companyRules)[task.TaskType];
        if (!effective.RequireEvidence)
            return;

        bool hasEvidence = await db.Attachments.AnyAsync(
            a => a.ModuleName == ComplianceEvidence.ModuleName && a.RecordId == task.Id, ct);
        if (hasEvidence)
            return;

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["Status"] =
            [
                $"ต้องแนบหลักฐาน (แบบที่ยื่น/ใบเสร็จ) ของงาน “{ComplianceTaskHelpers.TaskTypeName(task.TaskType)}” " +
                "ก่อนจึงจะปิดงานเป็น “เสร็จสิ้น” ได้",
            ],
        });
    }
}
