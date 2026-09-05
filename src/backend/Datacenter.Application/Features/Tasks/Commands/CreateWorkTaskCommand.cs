using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Commands;

public record CreateWorkTaskCommand(
    int ClientCompanyId,
    string Title,
    string? Description,
    string? Category,
    WorkTaskPriority Priority,
    DateTime? DueDate,
    int? AssignedUserId,
    WorkTaskRecurrence RecurrenceType = WorkTaskRecurrence.None,
    int RecurrenceInterval = 1,
    IReadOnlyList<WorkTaskItemInput>? Items = null)
    : IRequest<WorkTaskDto>, IRequireCompanyOwnerAccess;

public class CreateWorkTaskCommandValidator : AbstractValidator<CreateWorkTaskCommand>
{
    public CreateWorkTaskCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("ต้องระบุชื่องาน").MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).MaximumLength(100);
    }
}

public class CreateWorkTaskCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreateWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(CreateWorkTaskCommand request, CancellationToken ct)
    {
        var task = new WorkTask
        {
            ClientCompanyId = request.ClientCompanyId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            DueDate = request.DueDate,
            AssignedUserId = request.AssignedUserId,
            Status = WorkTaskStatus.Open,
            RecurrenceType = request.RecurrenceType,
            RecurrenceInterval = request.RecurrenceInterval < 1 ? 1 : request.RecurrenceInterval,
            CreatedBy = currentUser.Username,
        };
        int order = 0;
        foreach (var it in (request.Items ?? []).Where(i => !string.IsNullOrWhiteSpace(i.Text)))
            task.Items.Add(new WorkTaskItem { Text = it.Text.Trim(), IsDone = it.IsDone, SortOrder = order++, CreatedBy = currentUser.Username });

        db.WorkTasks.Add(task);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("CreateWorkTask", "WorkTask", task.Id.ToString(),
            afterValue: task.Title, companyId: task.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        var saved = await db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser).Include(t => t.CompletedByUser)
            .Include(t => t.Items)
            .FirstAsync(t => t.Id == task.Id, ct);
        return WorkTaskMapper.ToDto(saved, DateTime.UtcNow.Date);
    }
}
