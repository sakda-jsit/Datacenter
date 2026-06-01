using FluentValidation;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class GenerateMonthlyTasksCommandValidator : AbstractValidator<GenerateMonthlyTasksCommand>
{
    public GenerateMonthlyTasksCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
