using FluentValidation;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

public class GetComplianceTasksQueryValidator : AbstractValidator<GetComplianceTasksQuery>
{
    public GetComplianceTasksQueryValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).WithMessage("กรุณาเลือกบริษัทลูกค้า");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("ปีไม่ถูกต้อง");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue)
            .WithMessage("เดือนต้องอยู่ระหว่าง 1-12");
    }
}
