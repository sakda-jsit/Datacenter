using Datacenter.Application.Features.Adjustments.DTOs;
using FluentValidation;

namespace Datacenter.Application.Features.Adjustments.Commands;

/// <summary>กฎร่วมสำหรับ lines ของ adjustment (ใช้ทั้ง create/update)</summary>
public class AdjustmentLineValidator : AbstractValidator<AdjustmentLineInput>
{
    public AdjustmentLineValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชี");
        RuleFor(x => x.DebitAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CreditAmount).GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(l => (l.DebitAmount > 0) ^ (l.CreditAmount > 0))
            .WithMessage("แต่ละบรรทัดต้องมีเดบิตหรือเครดิตอย่างใดอย่างหนึ่ง (ไม่เป็นศูนย์ทั้งคู่และไม่มีทั้งคู่)");
    }
}

public static class AdjustmentBalanceRule
{
    /// <summary>รวมเดบิต = รวมเครดิต และมีอย่างน้อย 1 บรรทัด</summary>
    public static bool IsBalanced(IReadOnlyList<AdjustmentLineInput> lines)
        => lines.Count > 0
        && Math.Round(lines.Sum(l => l.DebitAmount) - lines.Sum(l => l.CreditAmount), 2) == 0;
}
