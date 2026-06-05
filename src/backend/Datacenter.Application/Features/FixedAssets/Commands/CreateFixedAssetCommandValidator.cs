using FluentValidation;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class CreateFixedAssetCommandValidator : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new FixedAssetInputValidator());
    }
}
