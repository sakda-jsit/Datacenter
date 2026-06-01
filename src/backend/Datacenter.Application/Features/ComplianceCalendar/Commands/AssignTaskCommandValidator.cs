using FluentValidation;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).GreaterThan(0).WithMessage("TaskId ไม่ถูกต้อง");
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue)
            .WithMessage("UserId ต้องมากกว่า 0");
    }
}
