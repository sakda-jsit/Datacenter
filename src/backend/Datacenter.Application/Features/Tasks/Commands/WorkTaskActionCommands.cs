using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Commands;

// ── เปลี่ยนสถานะเร็ว ─────────────────────────────────────────────────────────
public record UpdateWorkTaskStatusCommand(int Id, WorkTaskStatus Status) : IRequest<WorkTaskDto>;

public class UpdateWorkTaskStatusCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit, ICompanyAccessGuard accessGuard)
    : IRequestHandler<UpdateWorkTaskStatusCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(UpdateWorkTaskStatusCommand request, CancellationToken ct)
    {
        var task = await db.WorkTasks
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("WorkTask", request.Id);
        await accessGuard.EnsureOwnerAccessAsync(task.ClientCompanyId, ct);

        var prev = task.Status;
        task.Status = request.Status;
        if (request.Status == WorkTaskStatus.Done)
        {
            task.CompletedAt = DateTime.UtcNow;
            task.CompletedByUserId = currentUser.UserId;
        }
        else
        {
            task.CompletedAt = null;
            task.CompletedByUserId = null;
        }
        task.ModifiedBy = currentUser.Username;
        task.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("UpdateWorkTaskStatus", "WorkTask", task.Id.ToString(),
            beforeValue: prev.ToString(), afterValue: request.Status.ToString(),
            companyId: task.ClientCompanyId, cancellationToken: ct);

        // งานประจำ (recurring): เพิ่งปิดเป็น Done + ตั้งรอบไว้ → spawn งานถัดไป (Open) ครั้งเดียว
        if (request.Status == WorkTaskStatus.Done && prev != WorkTaskStatus.Done
            && task.RecurrenceType != WorkTaskRecurrence.None)
        {
            db.WorkTasks.Add(SpawnNext(task, currentUser.Username));
            await audit.LogAsync("SpawnRecurringWorkTask", "WorkTask", task.Id.ToString(),
                afterValue: $"recurrence={task.RecurrenceType}×{task.RecurrenceInterval}",
                companyId: task.ClientCompanyId, cancellationToken: ct);
        }

        await db.SaveChangesAsync(ct);

        return await ReloadDtoAsync(db, task.Id, ct);
    }

    /// <summary>สร้างงานถัดไปของงานประจำ: clone ข้อมูล + เลื่อนกำหนดส่งตามรอบ + checklist รีเซ็ตเป็นยังไม่เสร็จ</summary>
    private static WorkTask SpawnNext(WorkTask t, string username)
    {
        int n = t.RecurrenceInterval < 1 ? 1 : t.RecurrenceInterval;
        DateTime baseDate = t.DueDate ?? DateTime.UtcNow.Date;
        DateTime next = t.RecurrenceType switch
        {
            WorkTaskRecurrence.Daily => baseDate.AddDays(n),
            WorkTaskRecurrence.Weekly => baseDate.AddDays(7 * n),
            WorkTaskRecurrence.Monthly => baseDate.AddMonths(n),
            WorkTaskRecurrence.Yearly => baseDate.AddYears(n),
            _ => baseDate,
        };
        return new WorkTask
        {
            ClientCompanyId = t.ClientCompanyId,
            Title = t.Title,
            Description = t.Description,
            Category = t.Category,
            Priority = t.Priority,
            DueDate = next,
            AssignedUserId = t.AssignedUserId,
            Status = WorkTaskStatus.Open,
            RecurrenceType = t.RecurrenceType,
            RecurrenceInterval = n,
            CreatedBy = username,
            Items = t.Items.OrderBy(i => i.SortOrder)
                .Select(i => new WorkTaskItem { Text = i.Text, IsDone = false, SortOrder = i.SortOrder, CreatedBy = username })
                .ToList(),
        };
    }

    internal static async Task<WorkTaskDto> ReloadDtoAsync(IApplicationDbContext db, int id, CancellationToken ct)
    {
        var saved = await db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .Include(t => t.Items)
            .FirstAsync(t => t.Id == id, ct);
        return WorkTaskMapper.ToDto(saved, DateTime.UtcNow.Date);
    }
}

// ── มอบหมายผู้รับผิดชอบ ──────────────────────────────────────────────────────
public record AssignWorkTaskCommand(int Id, int? UserId) : IRequest<WorkTaskDto>;

public class AssignWorkTaskCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit, ICompanyAccessGuard accessGuard)
    : IRequestHandler<AssignWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(AssignWorkTaskCommand request, CancellationToken ct)
    {
        var task = await db.WorkTasks
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("WorkTask", request.Id);
        await accessGuard.EnsureOwnerAccessAsync(task.ClientCompanyId, ct);

        var prev = task.AssignedUserId;
        task.AssignedUserId = request.UserId;
        task.ModifiedBy = currentUser.Username;
        task.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("AssignWorkTask", "WorkTask", task.Id.ToString(),
            beforeValue: prev?.ToString(), afterValue: request.UserId?.ToString(),
            companyId: task.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return await UpdateWorkTaskStatusCommandHandler.ReloadDtoAsync(db, task.Id, ct);
    }
}

// ── ลบงาน ───────────────────────────────────────────────────────────────────
public record DeleteWorkTaskCommand(int Id) : IRequest;

public class DeleteWorkTaskCommandHandler(
    IApplicationDbContext db, IAuditService audit, ICompanyAccessGuard accessGuard)
    : IRequestHandler<DeleteWorkTaskCommand>
{
    public async Task Handle(DeleteWorkTaskCommand request, CancellationToken ct)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("WorkTask", request.Id);
        await accessGuard.EnsureOwnerAccessAsync(task.ClientCompanyId, ct);

        db.WorkTasks.Remove(task);
        await audit.LogAsync("DeleteWorkTask", "WorkTask", task.Id.ToString(),
            beforeValue: task.Title, companyId: task.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}

// ── ติ๊ก/ยกเลิกติ๊ก รายการย่อย (checklist) ──────────────────────────────────────
public record ToggleWorkTaskItemCommand(int TaskId, int ItemId, bool IsDone) : IRequest<WorkTaskDto>;

public class ToggleWorkTaskItemCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, ICompanyAccessGuard accessGuard)
    : IRequestHandler<ToggleWorkTaskItemCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(ToggleWorkTaskItemCommand request, CancellationToken ct)
    {
        var item = await db.WorkTaskItems
            .Include(i => i.WorkTask)
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.WorkTaskId == request.TaskId, ct)
            ?? throw new NotFoundException("WorkTaskItem", request.ItemId);
        await accessGuard.EnsureOwnerAccessAsync(item.WorkTask.ClientCompanyId, ct);

        item.IsDone = request.IsDone;
        item.ModifiedBy = currentUser.Username;
        item.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await UpdateWorkTaskStatusCommandHandler.ReloadDtoAsync(db, request.TaskId, ct);
    }
}
