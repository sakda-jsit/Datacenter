using FluentValidation;

namespace Datacenter.Application.Features.Import.Commands;

public class StartExpressImportCommandValidator : AbstractValidator<StartExpressImportCommand>
{
    public StartExpressImportCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0).WithMessage("กรุณาเลือกบริษัทลูกค้า");
        RuleFor(x => x.FiscalYear)
            .InclusiveBetween(2000, 2100).WithMessage("ปีบัญชีไม่ถูกต้อง");
    }
}
