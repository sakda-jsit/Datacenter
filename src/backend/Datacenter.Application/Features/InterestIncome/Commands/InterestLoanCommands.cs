using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.InterestIncome.DTOs;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.InterestIncome.Commands;

// ── Create ──────────────────────────────────────────────────────────────────────
public record CreateInterestLoanCommand(int ClientCompanyId, InterestLoanInput Data)
    : IRequest<InterestLoanDto>, IRequireCompanyAccess;

public class CreateInterestLoanCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreateInterestLoanCommand, InterestLoanDto>
{
    public async Task<InterestLoanDto> Handle(CreateInterestLoanCommand request, CancellationToken ct)
    {
        var accounts = await InterestLoanAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId,
            new[] { request.Data.InterestReceivableAccountId, request.Data.InterestIncomeAccountId }, ct);

        var entity = new InterestBearingLoan { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
        InterestLoanMapper.Apply(entity, request.Data);
        db.InterestBearingLoans.Add(entity);

        await audit.LogAsync("Create", "InterestBearingLoan",
            entityId: $"{request.ClientCompanyId}:{entity.Name}",
            afterValue: $"{entity.Name} / อัตรา {entity.AnnualRatePct:N2}%",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return InterestLoanMapper.ToDto(entity, accounts);
    }
}

// ── Update ──────────────────────────────────────────────────────────────────────
public record UpdateInterestLoanCommand(int Id, int ClientCompanyId, InterestLoanInput Data)
    : IRequest<InterestLoanDto>, IRequireCompanyAccess;

public class UpdateInterestLoanCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpdateInterestLoanCommand, InterestLoanDto>
{
    public async Task<InterestLoanDto> Handle(UpdateInterestLoanCommand request, CancellationToken ct)
    {
        var entity = await db.InterestBearingLoans.Include(x => x.Movements)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("InterestBearingLoan", request.Id);

        var accounts = await InterestLoanAccountGuard.LoadAndValidateAsync(db, request.ClientCompanyId,
            new[] { request.Data.InterestReceivableAccountId, request.Data.InterestIncomeAccountId }, ct);

        InterestLoanMapper.Apply(entity, request.Data);
        entity.ModifiedBy = currentUser.Username;
        entity.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("Update", "InterestBearingLoan",
            entityId: $"{request.ClientCompanyId}:{entity.Name}",
            afterValue: $"{entity.Name} / อัตรา {entity.AnnualRatePct:N2}%",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return InterestLoanMapper.ToDto(entity, accounts);
    }
}

// ── Delete ──────────────────────────────────────────────────────────────────────
public record DeleteInterestLoanCommand(int Id, int ClientCompanyId) : IRequest, IRequireCompanyAccess;

public class DeleteInterestLoanCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteInterestLoanCommand>
{
    public async Task Handle(DeleteInterestLoanCommand request, CancellationToken ct)
    {
        var entity = await db.InterestBearingLoans
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("InterestBearingLoan", request.Id);

        db.InterestBearingLoans.Remove(entity);
        await audit.LogAsync("Delete", "InterestBearingLoan",
            entityId: $"{request.ClientCompanyId}:{entity.Name}",
            beforeValue: entity.Name,
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}

// ── Validators ────────────────────────────────────────────────────────────────────
public class InterestLoanInputValidator : AbstractValidator<InterestLoanInput>
{
    public InterestLoanInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("ต้องระบุชื่อ/ผู้กู้").MaximumLength(200);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.AttachmentPath).MaximumLength(500);
        RuleFor(x => x.AnnualRatePct).GreaterThanOrEqualTo(0).WithMessage("อัตราดอกเบี้ยต้องไม่ติดลบ");
        RuleFor(x => x.SbtRatePct).GreaterThanOrEqualTo(0);
        RuleFor(x => x.LocalTaxPctOfSbt).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DayCountBasis).InclusiveBetween(360, 366).WithMessage("ฐานวันต่อปีต้องอยู่ระหว่าง 360–366");
        RuleFor(x => x.InterestReceivableAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีดอกเบี้ยค้างรับ");
        RuleFor(x => x.InterestIncomeAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีรายได้ดอกเบี้ย");
        RuleFor(x => x.Movements).NotEmpty().WithMessage("ต้องมีรายการเงินต้นอย่างน้อย 1 รายการ");
    }
}

public class CreateInterestLoanCommandValidator : AbstractValidator<CreateInterestLoanCommand>
{
    public CreateInterestLoanCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new InterestLoanInputValidator());
    }
}

public class UpdateInterestLoanCommandValidator : AbstractValidator<UpdateInterestLoanCommand>
{
    public UpdateInterestLoanCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new InterestLoanInputValidator());
    }
}
