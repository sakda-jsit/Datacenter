using FluentValidation;

namespace Datacenter.Application.Features.Import.Commands;

public class PostImportBatchCommandValidator : AbstractValidator<PostImportBatchCommand>
{
    public PostImportBatchCommandValidator()
    {
        RuleFor(x => x.ImportBatchId).GreaterThan(0).WithMessage("ImportBatchId ไม่ถูกต้อง");
    }
}
