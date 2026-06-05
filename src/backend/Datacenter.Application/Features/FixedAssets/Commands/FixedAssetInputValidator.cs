using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Enums;
using FluentValidation;

namespace Datacenter.Application.Features.FixedAssets.Commands;

/// <summary>กฎร่วมของฟิลด์สินทรัพย์ (ใช้ทั้ง create/update)</summary>
public class FixedAssetInputValidator : AbstractValidator<FixedAssetInput>
{
    public FixedAssetInputValidator()
    {
        RuleFor(x => x.AssetCode).NotEmpty().WithMessage("ต้องระบุรหัสสินทรัพย์").MaximumLength(50);
        RuleFor(x => x.AssetName).NotEmpty().WithMessage("ต้องระบุชื่อสินทรัพย์").MaximumLength(200);
        RuleFor(x => x.DisposalNote).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.AttachmentPath).MaximumLength(500);

        RuleFor(x => x.Cost).GreaterThan(0).WithMessage("ราคาทุนต้องมากกว่า 0");
        RuleFor(x => x.SalvageValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalvageValue).LessThanOrEqualTo(x => x.Cost)
            .WithMessage("มูลค่าซากต้องไม่เกินราคาทุน");

        RuleFor(x => x.BookRatePct).InclusiveBetween(0, 100).WithMessage("อัตราค่าเสื่อมบัญชีต้องอยู่ระหว่าง 0–100%");
        RuleFor(x => x.TaxRatePct).InclusiveBetween(0, 100).WithMessage("อัตราค่าเสื่อมภาษีต้องอยู่ระหว่าง 0–100%");

        RuleFor(x => x.AccumDepreciationAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีค่าเสื่อมราคาสะสม");
        RuleFor(x => x.DepreciationExpenseAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีค่าเสื่อมราคา");

        // จำหน่าย/ขาย/ตัดจำหน่าย ต้องมีวันที่จำหน่าย (req v11 docs/14: disposal validation)
        RuleFor(x => x.DisposalDate)
            .NotNull().WithMessage("รายการจำหน่าย/ขาย/ตัดจำหน่ายต้องระบุวันที่จำหน่าย")
            .When(x => x.Status != FixedAssetStatus.Active);

        RuleFor(x => x.DisposalDate)
            .GreaterThanOrEqualTo(x => x.AcquireDate)
            .WithMessage("วันที่จำหน่ายต้องไม่ก่อนวันที่ได้มา")
            .When(x => x.DisposalDate.HasValue);

        // ขาย (Sold) ต้องมีราคาขาย เพื่อคำนวณกำไร/ขาดทุน
        RuleFor(x => x.DisposalProceeds)
            .NotNull().WithMessage("การขายสินทรัพย์ต้องระบุราคาขาย")
            .When(x => x.Status == FixedAssetStatus.Sold);

        RuleFor(x => x.DisposalProceeds)
            .GreaterThanOrEqualTo(0).When(x => x.DisposalProceeds.HasValue);
    }
}
