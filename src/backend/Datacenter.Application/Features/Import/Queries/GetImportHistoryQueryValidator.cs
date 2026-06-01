using FluentValidation;

namespace Datacenter.Application.Features.Import.Queries;

public class GetImportHistoryQueryValidator : AbstractValidator<GetImportHistoryQuery>
{
    public GetImportHistoryQueryValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).When(x => x.ClientCompanyId.HasValue);
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100).When(x => x.FiscalYear.HasValue);
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("PageNumber ต้องไม่น้อยกว่า 1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("PageSize ต้องอยู่ระหว่าง 1-200");
    }
}
