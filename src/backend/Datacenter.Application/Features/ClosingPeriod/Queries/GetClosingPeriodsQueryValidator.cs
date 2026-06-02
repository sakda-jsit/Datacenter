using FluentValidation;

namespace Datacenter.Application.Features.ClosingPeriod.Queries;

public class GetClosingPeriodsQueryValidator : AbstractValidator<GetClosingPeriodsQuery>
{
    public GetClosingPeriodsQueryValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).WithMessage("กรุณาเลือกบริษัทลูกค้า");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("ปีไม่ถูกต้อง");
    }
}
