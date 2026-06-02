using FluentValidation;

namespace Datacenter.Application.Features.AuditLog.Queries;

public class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("หน้าต้องมากกว่า 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).WithMessage("ขนาดหน้าต้องอยู่ระหว่าง 1-200");
        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("วันสิ้นสุดต้องไม่ก่อนวันเริ่มต้น");
    }
}
