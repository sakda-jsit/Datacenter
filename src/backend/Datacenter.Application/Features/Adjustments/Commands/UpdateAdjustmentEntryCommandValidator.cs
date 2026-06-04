using FluentValidation;

namespace Datacenter.Application.Features.Adjustments.Commands;

public class UpdateAdjustmentEntryCommandValidator : AbstractValidator<UpdateAdjustmentEntryCommand>
{
    public UpdateAdjustmentEntryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("ต้องระบุเหตุผลการปรับปรุง").MaximumLength(500);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.AttachmentPath).MaximumLength(500);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("ต้องมีอย่างน้อย 1 บรรทัด");
        RuleForEach(x => x.Lines).SetValidator(new AdjustmentLineValidator());

        RuleFor(x => x.Lines)
            .Must(AdjustmentBalanceRule.IsBalanced)
            .WithMessage("รายการปรับปรุงต้องสมดุล (รวมเดบิต = รวมเครดิต)")
            .When(x => x.Lines is { Count: > 0 });
    }
}
