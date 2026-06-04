using FluentValidation;

namespace Datacenter.Application.Features.Leasing.Commands;

public class UpdateLeaseContractCommandValidator : AbstractValidator<UpdateLeaseContractCommand>
{
    public UpdateLeaseContractCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new LeaseContractInputValidator());
    }
}
