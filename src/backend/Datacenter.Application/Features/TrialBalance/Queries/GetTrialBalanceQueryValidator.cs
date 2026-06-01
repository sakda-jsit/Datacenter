using FluentValidation;

namespace Datacenter.Application.Features.TrialBalance.Queries;

public class GetTrialBalanceQueryValidator : AbstractValidator<GetTrialBalanceQuery>
{
    public GetTrialBalanceQueryValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).WithMessage("กรุณาเลือกบริษัทลูกค้า");
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100).WithMessage("ปีบัญชีไม่ถูกต้อง");
        RuleFor(x => x.MonthFrom).InclusiveBetween(1, 12).When(x => x.MonthFrom.HasValue)
            .WithMessage("เดือนเริ่มต้นต้องอยู่ระหว่าง 1-12");
        RuleFor(x => x.MonthTo).InclusiveBetween(1, 12).When(x => x.MonthTo.HasValue)
            .WithMessage("เดือนสิ้นสุดต้องอยู่ระหว่าง 1-12");
        RuleFor(x => x).Must(x => !x.MonthFrom.HasValue || !x.MonthTo.HasValue || x.MonthFrom <= x.MonthTo)
            .WithMessage("เดือนเริ่มต้นต้องไม่มากกว่าเดือนสิ้นสุด");
    }
}
