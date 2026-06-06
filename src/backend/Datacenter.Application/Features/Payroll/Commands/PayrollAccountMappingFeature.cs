using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// แมพบัญชีเงินเดือน Express → ฝ่าย (ต่อบริษัท) — ใช้ระบุว่า import พนักงานจากบัญชีไหน

public record GetPayrollAccountMappingsQuery(int ClientCompanyId)
    : IRequest<IReadOnlyList<PayrollAccountMappingDto>>, IRequireCompanyAccess;

public class GetPayrollAccountMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPayrollAccountMappingsQuery, IReadOnlyList<PayrollAccountMappingDto>>
{
    public async Task<IReadOnlyList<PayrollAccountMappingDto>> Handle(GetPayrollAccountMappingsQuery request, CancellationToken ct)
        => await db.PayrollAccountMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .OrderBy(m => m.AccountCode)
            .Select(m => new PayrollAccountMappingDto(m.Id, m.AccountCode, m.Department, m.Note))
            .ToListAsync(ct);
}

public record UpsertPayrollAccountMappingCommand(int ClientCompanyId, int? Id, PayrollAccountMappingInput Data)
    : IRequest<int>, IRequireCompanyAccess;

public class UpsertPayrollAccountMappingCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertPayrollAccountMappingCommand, int>
{
    public async Task<int> Handle(UpsertPayrollAccountMappingCommand request, CancellationToken ct)
    {
        var code = request.Data.AccountCode.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Data.Department))
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                "AccountCode", "กรุณาระบุรหัสบัญชีและฝ่าย") });

        PayrollAccountMapping m;
        if (request.Id is { } id)
            m = await db.PayrollAccountMappings.FirstOrDefaultAsync(
                    x => x.Id == id && x.ClientCompanyId == request.ClientCompanyId, ct)
                ?? throw new NotFoundException("PayrollAccountMapping", id);
        else
        {
            bool dup = await db.PayrollAccountMappings.AnyAsync(
                x => x.ClientCompanyId == request.ClientCompanyId && x.AccountCode == code, ct);
            if (dup)
                throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure(
                    "AccountCode", $"บัญชี {code} ถูกแมพไว้แล้ว") });
            m = new PayrollAccountMapping { ClientCompanyId = request.ClientCompanyId, CreatedBy = currentUser.Username };
            db.PayrollAccountMappings.Add(m);
        }
        m.AccountCode = code;
        m.Department = request.Data.Department.Trim();
        m.Note = request.Data.Note;
        m.ModifiedBy = currentUser.Username;
        m.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return m.Id;
    }
}

public record DeletePayrollAccountMappingCommand(int ClientCompanyId, int Id)
    : IRequest<Unit>, IRequireCompanyAccess;

public class DeletePayrollAccountMappingCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePayrollAccountMappingCommand, Unit>
{
    public async Task<Unit> Handle(DeletePayrollAccountMappingCommand request, CancellationToken ct)
    {
        var m = await db.PayrollAccountMappings.FirstOrDefaultAsync(
                x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("PayrollAccountMapping", request.Id);
        db.PayrollAccountMappings.Remove(m);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
