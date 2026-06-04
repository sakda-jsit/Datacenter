using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Domain.Enums;
using FluentValidation;

namespace Datacenter.Application.Features.Leasing.Commands;

/// <summary>กฎร่วมของฟิลด์สัญญา (ใช้ทั้ง create/update)</summary>
public class LeaseContractInputValidator : AbstractValidator<LeaseContractInput>
{
    public LeaseContractInputValidator()
    {
        RuleFor(x => x.ContractNo).NotEmpty().WithMessage("ต้องระบุเลขที่สัญญา").MaximumLength(50);
        RuleFor(x => x.AssetName).NotEmpty().WithMessage("ต้องระบุชื่อทรัพย์สิน/รายการ").MaximumLength(200);
        RuleFor(x => x.AssetCode).MaximumLength(50);
        RuleFor(x => x.Lessor).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.AttachmentPath).MaximumLength(500);

        RuleFor(x => x.NumberOfPeriods).InclusiveBetween(1, 600).WithMessage("จำนวนงวดต้องอยู่ระหว่าง 1–600");
        RuleFor(x => x.PaymentsPerYear).InclusiveBetween(1, 12).WithMessage("จำนวนงวดต่อปีต้องอยู่ระหว่าง 1–12");
        RuleFor(x => x.FinancedPrincipal).GreaterThan(0).WithMessage("เงินต้นที่จัดไฟแนนซ์ต้องมากกว่า 0");
        RuleFor(x => x.InstallmentAmount).GreaterThan(0).WithMessage("ค่างวดต้องมากกว่า 0");
        RuleFor(x => x.CashPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DownPayment).GreaterThanOrEqualTo(0);
        RuleFor(x => x.VatPerPeriod).GreaterThanOrEqualTo(0);

        RuleFor(x => x.LiabilityAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีหนี้สิน");
        RuleFor(x => x.InterestExpenseAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีดอกเบี้ยจ่าย");

        // ค่างวด × จำนวนงวด ต้องคุ้มเงินต้น (มีดอกเบี้ย ≥ 0)
        RuleFor(x => x)
            .Must(x => x.InstallmentAmount * x.NumberOfPeriods >= x.FinancedPrincipal)
            .WithMessage("ค่างวด × จำนวนงวด ต้องไม่น้อยกว่าเงินต้น (ดอกเบี้ยติดลบไม่ได้)");

        // เช่าซื้อควรมีบัญชีดอกเบี้ยรอตัด/ภาษีซื้อ (เตือนผ่าน rule แยกเฉพาะเมื่อ VAT > 0)
        RuleFor(x => x.InputVatUndueAccountId)
            .NotNull().WithMessage("เช่าซื้อที่มี VAT ต้องระบุบัญชีภาษีซื้อยังไม่ถึงกำหนด")
            .When(x => x.ContractType == LeaseContractType.HirePurchase && x.VatPerPeriod > 0);
    }
}
