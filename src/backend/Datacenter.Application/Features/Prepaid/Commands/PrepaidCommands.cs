using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Prepaid.DTOs;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Prepaid.Commands;

// ── Create ──────────────────────────────────────────────────────────────────────
public record CreatePrepaidExpenseCommand(int ClientCompanyId, PrepaidExpenseInput Data)
    : IRequest<PrepaidExpenseDto>, IRequireCompanyAccess;

public class CreatePrepaidExpenseCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreatePrepaidExpenseCommand, PrepaidExpenseDto>
{
    public async Task<PrepaidExpenseDto> Handle(CreatePrepaidExpenseCommand request, CancellationToken ct)
    {
        var accounts = await PrepaidAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, request.Data, ct);

        var entity = new PrepaidExpense { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
        PrepaidMapper.Apply(entity, request.Data);
        db.PrepaidExpenses.Add(entity);

        await audit.LogAsync("Create", "PrepaidExpense",
            entityId: $"{request.ClientCompanyId}:{entity.Name}",
            afterValue: $"{entity.Name} / {entity.TotalAmount:N2} ({entity.StartDate:yyyy-MM-dd}–{entity.EndDate:yyyy-MM-dd})",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return PrepaidMapper.ToDto(entity, accounts);
    }
}

// ── Update ──────────────────────────────────────────────────────────────────────
public record UpdatePrepaidExpenseCommand(int Id, int ClientCompanyId, PrepaidExpenseInput Data)
    : IRequest<PrepaidExpenseDto>, IRequireCompanyAccess;

public class UpdatePrepaidExpenseCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpdatePrepaidExpenseCommand, PrepaidExpenseDto>
{
    public async Task<PrepaidExpenseDto> Handle(UpdatePrepaidExpenseCommand request, CancellationToken ct)
    {
        var entity = await db.PrepaidExpenses
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PrepaidExpense", request.Id);

        var accounts = await PrepaidAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId, request.Data, ct);

        PrepaidMapper.Apply(entity, request.Data);
        entity.ModifiedBy = currentUser.Username;
        entity.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("Update", "PrepaidExpense",
            entityId: $"{request.ClientCompanyId}:{entity.Name}",
            afterValue: $"{entity.Name} / {entity.TotalAmount:N2}",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return PrepaidMapper.ToDto(entity, accounts);
    }
}

// ── Delete ──────────────────────────────────────────────────────────────────────
public record DeletePrepaidExpenseCommand(int Id, int ClientCompanyId) : IRequest, IRequireCompanyAccess;

public class DeletePrepaidExpenseCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeletePrepaidExpenseCommand>
{
    public async Task Handle(DeletePrepaidExpenseCommand request, CancellationToken ct)
    {
        var entity = await db.PrepaidExpenses
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PrepaidExpense", request.Id);

        db.PrepaidExpenses.Remove(entity);
        await audit.LogAsync("Delete", "PrepaidExpense",
            entityId: $"{request.ClientCompanyId}:{entity.Name}",
            beforeValue: $"{entity.Name} / {entity.TotalAmount:N2}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}

// ── Validators ────────────────────────────────────────────────────────────────────
public class PrepaidExpenseInputValidator : AbstractValidator<PrepaidExpenseInput>
{
    public PrepaidExpenseInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("ต้องระบุรายละเอียด").MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.AttachmentPath).MaximumLength(500);
        RuleFor(x => x.TotalAmount).GreaterThan(0).WithMessage("มูลค่าตั้งต้นต้องมากกว่า 0");
        RuleFor(x => x.PrepaidAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีค่าใช้จ่ายจ่ายล่วงหน้า");
        RuleFor(x => x.ExpenseAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีค่าใช้จ่าย");
        RuleFor(x => x)
            .Must(x => x.StartDate.Date <= x.EndDate.Date)
            .WithMessage("วันเริ่มต้องไม่เกินวันสิ้นสุด");
    }
}

public class CreatePrepaidExpenseCommandValidator : AbstractValidator<CreatePrepaidExpenseCommand>
{
    public CreatePrepaidExpenseCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new PrepaidExpenseInputValidator());
    }
}

public class UpdatePrepaidExpenseCommandValidator : AbstractValidator<UpdatePrepaidExpenseCommand>
{
    public UpdatePrepaidExpenseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new PrepaidExpenseInputValidator());
    }
}
