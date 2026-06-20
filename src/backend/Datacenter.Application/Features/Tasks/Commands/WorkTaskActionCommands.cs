using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Tasks.DTOs;
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
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("WorkTask", request.Id);
        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

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
        await db.SaveChangesAsync(ct);

        return await ReloadDtoAsync(db, task.Id, ct);
    }

    internal static async Task<WorkTaskDto> ReloadDtoAsync(IApplicationDbContext db, int id, CancellationToken ct)
    {
        var saved = await db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
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
        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

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
        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

        db.WorkTasks.Remove(task);
        await audit.LogAsync("DeleteWorkTask", "WorkTask", task.Id.ToString(),
            beforeValue: task.Title, companyId: task.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}
