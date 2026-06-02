using FluentValidation;

namespace Datacenter.Application.Features.Import.Commands;

public class DeleteImportBatchCommandValidator : AbstractValidator<DeleteImportBatchCommand>
{
    public DeleteImportBatchCommandValidator()
    {
        RuleFor(x => x.ImportBatchId).GreaterThan(0).WithMessage("ImportBatchId ไม่ถูกต้อง");
    }
}
