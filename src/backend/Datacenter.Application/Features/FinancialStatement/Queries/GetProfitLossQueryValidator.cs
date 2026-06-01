using FluentValidation;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetProfitLossQueryValidator : AbstractValidator<GetProfitLossQuery>
{
    public GetProfitLossQueryValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).WithMessage("กรุณาเลือกบริษัทลูกค้า");
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100).WithMessage("ปีบัญชีไม่ถูกต้อง");
        RuleFor(x => x.MonthFrom).InclusiveBetween(1, 12).When(x => x.MonthFrom.HasValue);
        RuleFor(x => x.MonthTo).InclusiveBetween(1, 12).When(x => x.MonthTo.HasValue);
        RuleFor(x => x).Must(x => !x.MonthFrom.HasValue || !x.MonthTo.HasValue || x.MonthFrom <= x.MonthTo)
            .WithMessage("เดือนเริ่มต้นต้องไม่มากกว่าเดือนสิ้นสุด");
    }
}
