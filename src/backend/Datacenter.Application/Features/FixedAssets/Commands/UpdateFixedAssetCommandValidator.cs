using FluentValidation;

namespace Datacenter.Application.Features.FixedAssets.Commands;

public class UpdateFixedAssetCommandValidator : AbstractValidator<UpdateFixedAssetCommand>
{
    public UpdateFixedAssetCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new FixedAssetInputValidator());
    }
}
