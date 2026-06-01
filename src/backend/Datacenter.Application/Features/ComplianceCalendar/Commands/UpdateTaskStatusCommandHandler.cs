using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Enums;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class UpdateTaskStatusCommandHandler(IApplicationDbContext db, IAuditService audit, ICompanyAccessGuard accessGuard)
    : IRequestHandler<UpdateTaskStatusCommand>
{
    public async Task Handle(UpdateTaskStatusCommand request, CancellationToken ct)
    {
        var task = await db.ComplianceTasks.FindAsync([request.TaskId], ct)
            ?? throw new NotFoundException("ComplianceTask", request.TaskId);

        // task อ้างถึงบริษัทผ่าน TaskId จึงตรวจสิทธิ์หลังโหลด entity แทน pipeline behaviour
        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

        var previousStatus = task.Status;

        task.Status = request.Status;
        if (request.Note is not null)
            task.Note = request.Note;

        if (request.Status == ComplianceTaskStatus.Completed)
            task.CompletedAt = DateTime.UtcNow;
        else
            task.CompletedAt = null;

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
}
