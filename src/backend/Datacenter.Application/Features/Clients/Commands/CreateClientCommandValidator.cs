using FluentValidation;

namespace Datacenter.Application.Features.Clients.Commands;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("กรุณาระบุรหัสลูกค้า")
            .MaximumLength(20).WithMessage("รหัสลูกค้าต้องไม่เกิน 20 ตัวอักษร")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("รหัสลูกค้าใช้ได้เฉพาะ A-Z, 0-9, -, _");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("กรุณาระบุชื่อลูกค้า")
            .MaximumLength(200).WithMessage("ชื่อลูกค้าต้องไม่เกิน 200 ตัวอักษร");

        RuleFor(x => x.TaxId)
            .NotEmpty().WithMessage("กรุณาระบุเลขประจำตัวผู้เสียภาษี")
            .Length(13).WithMessage("เลขประจำตัวผู้เสียภาษีต้องมี 13 หลัก")
            .Matches(@"^\d{13}$").WithMessage("เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลขเท่านั้น");

        RuleFor(x => x.BranchCode)
            .NotEmpty().WithMessage("กรุณาระบุรหัสสาขา")
            .Length(5).WithMessage("รหัสสาขาต้องมี 5 หลัก")
            .Matches(@"^\d{5}$").WithMessage("รหัสสาขาต้องเป็นตัวเลขเท่านั้น");

        RuleFor(x => x.FiscalYearStartMonth)
            .InclusiveBetween(1, 12).WithMessage("เดือนเริ่มต้นปีบัญชีต้องอยู่ระหว่าง 1-12");
    }
}
