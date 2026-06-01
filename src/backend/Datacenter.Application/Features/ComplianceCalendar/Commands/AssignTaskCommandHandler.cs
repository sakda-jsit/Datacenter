using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class AssignTaskCommandHandler(IApplicationDbContext db, IAuditService audit, ICompanyAccessGuard accessGuard)
    : IRequestHandler<AssignTaskCommand>
{
    public async Task Handle(AssignTaskCommand request, CancellationToken ct)
    {
        var task = await db.ComplianceTasks.FindAsync([request.TaskId], ct)
            ?? throw new NotFoundException("ComplianceTask", request.TaskId);

        // task อ้างถึงบริษัทผ่าน TaskId จึงตรวจสิทธิ์หลังโหลด entity แทน pipeline behaviour
        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

        var previousUserId = task.AssignedUserId;
        task.AssignedUserId = request.UserId;

        await audit.LogAsync(
            action: "Assign",
            entityName: "ComplianceTask",
            entityId: task.Id.ToString(),
            beforeValue: previousUserId?.ToString(),
            afterValue: request.UserId?.ToString(),
            companyId: task.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
    }
}
