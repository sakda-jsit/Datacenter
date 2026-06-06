using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── Create ─────────────────────────────────────────────────────────────────────
public record CreateEmployeeCommand(int ClientCompanyId, EmployeeInput Data)
    : IRequest<int>, IRequireCompanyAccess;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Data.EmployeeCode).NotEmpty().WithMessage("กรุณาระบุรหัสพนักงาน").MaximumLength(30);
        RuleFor(x => x.Data.FirstName).NotEmpty().WithMessage("กรุณาระบุชื่อ").MaximumLength(100);
        RuleFor(x => x.Data.NationalId).MaximumLength(20);
        RuleFor(x => x.Data.BaseSalary).GreaterThanOrEqualTo(0);
    }
}

public class CreateEmployeeCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateEmployeeCommand, int>
{
    public async Task<int> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var code = request.Data.EmployeeCode.Trim();
        bool dup = await db.Employees.AnyAsync(
            e => e.ClientCompanyId == request.ClientCompanyId && e.EmployeeCode == code, ct);
        if (dup)
            throw new Datacenter.Application.Common.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "EmployeeCode", $"รหัสพนักงาน {code} มีอยู่แล้วในบริษัทนี้") });

        var e = new Employee { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
        Apply(e, request.Data);
        db.Employees.Add(e);
        await db.SaveChangesAsync(ct);
        return e.Id;
    }

    internal static void Apply(Employee e, EmployeeInput d)
    {
        e.EmployeeCode = d.EmployeeCode.Trim();
        e.NationalId = (d.NationalId ?? "").Trim();
        e.Prefix = d.Prefix?.Trim();
        e.FirstName = d.FirstName.Trim();
        e.LastName = (d.LastName ?? "").Trim();
        e.BirthDate = d.BirthDate;
        e.MaritalStatus = d.MaritalStatus?.Trim();
        e.Nationality = d.Nationality?.Trim();
        e.Address = d.Address?.Trim();
        e.Position = d.Position?.Trim();
        e.Department = d.Department?.Trim();
        e.StartDate = d.StartDate;
        e.ResignDate = d.ResignDate;
        e.EmploymentStatus = d.EmploymentStatus;
        e.SalaryType = d.SalaryType;
        e.BaseSalary = d.BaseSalary;
        e.DailyWage = d.DailyWage;
        e.SsoNumber = d.SsoNumber?.Trim();
        e.SsoHospital = d.SsoHospital?.Trim();
        e.SsoStatus = d.SsoStatus;
        e.TaxId = d.TaxId?.Trim();
        e.Note = d.Note?.Trim();
    }
}

// ── Update ─────────────────────────────────────────────────────────────────────
public record UpdateEmployeeCommand(int Id, int ClientCompanyId, EmployeeInput Data)
    : IRequest<Unit>, IRequireCompanyAccess;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Data.EmployeeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Data.FirstName).NotEmpty().MaximumLength(100);
    }
}

public class UpdateEmployeeCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateEmployeeCommand, Unit>
{
    public async Task<Unit> Handle(UpdateEmployeeCommand request, CancellationToken ct)
    {
        var e = await db.Employees.FirstOrDefaultAsync(
            x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("Employee", request.Id);

        var code = request.Data.EmployeeCode.Trim();
        bool dup = await db.Employees.AnyAsync(
            x => x.ClientCompanyId == request.ClientCompanyId && x.EmployeeCode == code && x.Id != request.Id, ct);
        if (dup)
            throw new Datacenter.Application.Common.Exceptions.ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "EmployeeCode", $"รหัสพนักงาน {code} มีอยู่แล้วในบริษัทนี้") });

        CreateEmployeeCommandHandler.Apply(e, request.Data);
        e.ModifiedBy = currentUser.Username;
        e.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

// ── Delete ─────────────────────────────────────────────────────────────────────
public record DeleteEmployeeCommand(int Id, int ClientCompanyId)
    : IRequest<Unit>, IRequireCompanyAccess;

public class DeleteEmployeeCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteEmployeeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteEmployeeCommand request, CancellationToken ct)
    {
        var e = await db.Employees.FirstOrDefaultAsync(
            x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("Employee", request.Id);

        db.Employees.Remove(e); // cascade ลบ documents + enrollments
        await audit.LogAsync("Delete", "Employee",
            entityId: $"{e.ClientCompanyId}:{e.EmployeeCode}",
            beforeValue: $"{e.Prefix} {e.FirstName} {e.LastName}".Trim(),
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
