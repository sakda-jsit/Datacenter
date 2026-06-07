using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.CashCount.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainCashCount = Datacenter.Domain.Entities.CashCount;

namespace Datacenter.Application.Features.CashCount.Commands;

// ── Create ──────────────────────────────────────────────────────────────────────
public record CreateCashCountCommand(int ClientCompanyId, CashCountInput Data)
    : IRequest<CashCountDto>, IRequireCompanyAccess;

public class CreateCashCountCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<CreateCashCountCommand, CashCountDto>
{
    public async Task<CashCountDto> Handle(CreateCashCountCommand request, CancellationToken ct)
    {
        var accounts = await CashCountAccountGuard.LoadAndValidateAsync(
            db, request.ClientCompanyId, new[] { request.Data.CashAccountId }, ct);

        var entity = new DomainCashCount { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
        CashCountMapper.Apply(entity, request.Data);
        db.CashCounts.Add(entity);

        await audit.LogAsync("Create", "CashCount",
            entityId: $"{request.ClientCompanyId}:{entity.FiscalYear}:{entity.Reference}",
            afterValue: $"{entity.CountDate:yyyy-MM-dd} / นับได้ {CashCountMapper.CountedTotal(entity):N2}",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return CashCountMapper.ToDto(entity, accounts);
    }
}

// ── Update ──────────────────────────────────────────────────────────────────────
public record UpdateCashCountCommand(int Id, int ClientCompanyId, CashCountInput Data)
    : IRequest<CashCountDto>, IRequireCompanyAccess;

public class UpdateCashCountCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UpdateCashCountCommand, CashCountDto>
{
    public async Task<CashCountDto> Handle(UpdateCashCountCommand request, CancellationToken ct)
    {
        var entity = await db.CashCounts.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("CashCount", request.Id);

        var accounts = await CashCountAccountGuard.LoadAndValidateAsync(
            db, request.ClientCompanyId, new[] { request.Data.CashAccountId }, ct);

        CashCountMapper.Apply(entity, request.Data);
        entity.ModifiedBy = currentUser.Username;
        entity.ModifiedAt = DateTime.UtcNow;

        await audit.LogAsync("Update", "CashCount",
            entityId: $"{request.ClientCompanyId}:{entity.FiscalYear}:{entity.Reference}",
            afterValue: $"{entity.CountDate:yyyy-MM-dd} / นับได้ {CashCountMapper.CountedTotal(entity):N2}",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return CashCountMapper.ToDto(entity, accounts);
    }
}

// ── Delete ──────────────────────────────────────────────────────────────────────
public record DeleteCashCountCommand(int Id, int ClientCompanyId) : IRequest, IRequireCompanyAccess;

public class DeleteCashCountCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteCashCountCommand>
{
    public async Task Handle(DeleteCashCountCommand request, CancellationToken ct)
    {
        var entity = await db.CashCounts
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("CashCount", request.Id);

        db.CashCounts.Remove(entity);
        await audit.LogAsync("Delete", "CashCount",
            entityId: $"{request.ClientCompanyId}:{entity.FiscalYear}:{entity.Reference}",
            beforeValue: $"{entity.CountDate:yyyy-MM-dd}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}

// ── Validators ────────────────────────────────────────────────────────────────────
public class CashCountInputValidator : AbstractValidator<CashCountInput>
{
    public CashCountInputValidator()
    {
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Reference).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.AttachmentPath).MaximumLength(500);
        RuleFor(x => x.CashAccountId).GreaterThan(0).WithMessage("ต้องระบุบัญชีเงินสด");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("ต้องมีรายการนับอย่างน้อย 1 รายการ");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.Denomination).GreaterThan(0).WithMessage("มูลค่าหน้าตั๋วต้องมากกว่า 0");
            l.RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).WithMessage("จำนวนต้องไม่ติดลบ");
        });
    }
}

public class CreateCashCountCommandValidator : AbstractValidator<CreateCashCountCommand>
{
    public CreateCashCountCommandValidator()
    {
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new CashCountInputValidator());
    }
}

public class UpdateCashCountCommandValidator : AbstractValidator<UpdateCashCountCommand>
{
    public UpdateCashCountCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ClientCompanyId).GreaterThan(0);
        RuleFor(x => x.Data).NotNull().SetValidator(new CashCountInputValidator());
    }
}
