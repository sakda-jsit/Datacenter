using FluentValidation;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

public class GetBalanceSheetQueryValidator : AbstractValidator<GetBalanceSheetQuery>
{
    public GetBalanceSheetQueryValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).WithMessage("กรุณาเลือกบริษัทลูกค้า");
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100).WithMessage("ปีบัญชีไม่ถูกต้อง");
    }
}
