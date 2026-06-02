using FluentValidation;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

public class UpsertExternalInputCommandValidator : AbstractValidator<UpsertExternalInputCommand>
{
    // X4  = income tax expense (P&L deduction)
    // WHT = prepaid withholding tax applied against this year's income tax (balance-sheet settlement)
    private static readonly HashSet<string> AllowedRefCodes = ["X4", "WHT"];

    public UpsertExternalInputCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.RefCode).Must(c => AllowedRefCodes.Contains(c))
            .WithMessage($"RefCode ต้องเป็นหนึ่งใน: {string.Join(", ", AllowedRefCodes)}");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage("จำนวนเงินต้องไม่ติดลบ");
    }
}
