using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Commands;

public record UpdateWorkTaskCommand(
    int Id,
    string Title,
    string? Description,
    string? Category,
    WorkTaskPriority Priority,
    DateTime? DueDate,
    int? AssignedUserId,
    WorkTaskRecurrence RecurrenceType = WorkTaskRecurrence.None,
    int RecurrenceInterval = 1,
    IReadOnlyList<WorkTaskItemInput>? Items = null)
    : IRequest<WorkTaskDto>;

public class UpdateWorkTaskCommandValidator : AbstractValidator<UpdateWorkTaskCommand>
{
    public UpdateWorkTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("ต้องระบุชื่องาน").MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).MaximumLength(100);
    }
}

public class UpdateWorkTaskCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, ICompanyAccessGuard accessGuard)
    : IRequestHandler<UpdateWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(UpdateWorkTaskCommand request, CancellationToken ct)
    {
        var task = await db.WorkTasks
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("WorkTask", request.Id);

        await accessGuard.EnsureAccessAsync(task.ClientCompanyId, ct);

        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Category = request.Category;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.AssignedUserId = request.AssignedUserId;
        task.RecurrenceType = request.RecurrenceType;
        task.RecurrenceInterval = request.RecurrenceInterval < 1 ? 1 : request.RecurrenceInterval;
        task.ModifiedBy = currentUser.Username;
        task.ModifiedAt = DateTime.UtcNow;

        // checklist: replace-all (เหมือน adjustment lines) — ถ้าส่ง Items มา
        if (request.Items is not null)
        {
            db.WorkTaskItems.RemoveRange(task.Items);
            task.Items.Clear();
            int order = 0;
            foreach (var it in request.Items.Where(i => !string.IsNullOrWhiteSpace(i.Text)))
                task.Items.Add(new WorkTaskItem { Text = it.Text.Trim(), IsDone = it.IsDone, SortOrder = order++, CreatedBy = currentUser.Username });
        }

        await db.SaveChangesAsync(ct);

        var saved = await db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .Include(t => t.Items)
            .FirstAsync(t => t.Id == task.Id, ct);
        return WorkTaskMapper.ToDto(saved, DateTime.UtcNow.Date);
    }
}
