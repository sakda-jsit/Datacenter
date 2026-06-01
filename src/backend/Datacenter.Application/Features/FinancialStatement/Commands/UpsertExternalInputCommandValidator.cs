using FluentValidation;

namespace Datacenter.Application.Features.FinancialStatement.Commands;

public class UpsertExternalInputCommandValidator : AbstractValidator<UpsertExternalInputCommand>
{
    private static readonly HashSet<string> AllowedRefCodes = ["X4"];

    public UpsertExternalInputCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.RefCode).Must(c => AllowedRefCodes.Contains(c))
            .WithMessage($"RefCode ต้องเป็นหนึ่งใน: {string.Join(", ", AllowedRefCodes)}");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage("จำนวนเงินต้องไม่ติดลบ");
    }
}
