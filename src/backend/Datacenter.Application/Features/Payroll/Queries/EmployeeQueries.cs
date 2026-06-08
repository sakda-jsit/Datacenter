using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Payroll.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Queries;

// ── รายชื่อพนักงาน ───────────────────────────────────────────────────────────────
public record GetEmployeesQuery(int ClientCompanyId, bool IncludeResigned = true)
    : IRequest<IReadOnlyList<EmployeeListItemDto>>, IRequireCompanyAccess;

public class GetEmployeesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeListItemDto>>
{
    public async Task<IReadOnlyList<EmployeeListItemDto>> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        var q = db.Employees.AsNoTracking()
            .Where(e => e.ClientCompanyId == request.ClientCompanyId);
        if (!request.IncludeResigned)
            q = q.Where(e => e.EmploymentStatus == EmploymentStatus.Active);

        return await q
            .OrderBy(e => e.EmploymentStatus).ThenBy(e => e.EmployeeCode)
            .Select(e => new EmployeeListItemDto(
                e.Id, e.EmployeeCode,
                ((e.Prefix ?? "") + " " + e.FirstName + " " + e.LastName).Trim(),
                e.NationalId, e.Position, e.Department, e.StartDate, e.ResignDate,
                e.EmploymentStatus, e.SsoStatus, e.BaseSalary))
            .ToListAsync(ct);
    }
}

// ── รายละเอียดพนักงาน (+ เอกสาร + การแจ้ง ปกส.) ─────────────────────────────────
public record GetEmployeeQuery(int Id, int ClientCompanyId)
    : IRequest<EmployeeDetailDto>, IRequireCompanyAccess;

public class GetEmployeeQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEmployeeQuery, EmployeeDetailDto>
{
    public async Task<EmployeeDetailDto> Handle(GetEmployeeQuery request, CancellationToken ct)
    {
        var e = await db.Employees.AsNoTracking()
            .Include(x => x.Documents)
            .Include(x => x.SsoEnrollments)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("Employee", request.Id);

        var docs = e.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new EmployeeDocumentDto(
                d.Id, d.DocType, d.FileName, d.ContentType, d.EffectiveDate, d.Note, d.CreatedAt, d.CreatedBy))
            .ToList();

        var enr = e.SsoEnrollments
            .OrderByDescending(x => x.EventDate)
            .Select(x => new SsoEnrollmentDto(
                x.Id, x.Type, x.EventDate, x.SubmittedDate, x.Status, x.ProofDocumentId, x.Note))
            .ToList();

        return new EmployeeDetailDto(
            e.Id, e.ClientCompanyId, e.EmployeeCode, e.NationalId, e.Prefix, e.FirstName, e.LastName,
            e.BirthDate, e.MaritalStatus, e.Nationality, e.Address,
            Datacenter.Application.Features.Payroll.Services.EmployeeAddressMapper.ToDto(e),
            e.Position, e.Department, e.SourceSupplierCode,
            e.StartDate, e.ResignDate,
            e.EmploymentStatus, e.SalaryType, e.BaseSalary, e.DailyWage, e.SsoNumber, e.SsoHospital,
            e.SsoStatus, e.TaxId, e.Note, docs, enr);
    }
}
