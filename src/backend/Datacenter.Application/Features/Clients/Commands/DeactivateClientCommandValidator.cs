using FluentValidation;

namespace Datacenter.Application.Features.Clients.Commands;

public class DeactivateClientCommandValidator : AbstractValidator<DeactivateClientCommand>
{
    public DeactivateClientCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id บริษัทไม่ถูกต้อง");
    }
}
