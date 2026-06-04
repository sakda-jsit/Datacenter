using FluentValidation;

namespace Datacenter.Application.Features.Leasing.Commands;

public class CreateLeaseContractCommandValidator : AbstractValidator<CreateLeaseContractCommand>
{
    public CreateLeaseContractCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new LeaseContractInputValidator());
    }
}
