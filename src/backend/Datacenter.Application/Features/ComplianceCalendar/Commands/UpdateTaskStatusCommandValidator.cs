using FluentValidation;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class UpdateTaskStatusCommandValidator : AbstractValidator<UpdateTaskStatusCommand>
{
    public UpdateTaskStatusCommandValidator()
    {
        RuleFor(x => x.TaskId).GreaterThan(0).WithMessage("TaskId ไม่ถูกต้อง");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status ไม่ถูกต้อง");
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note is not null);
    }
}
