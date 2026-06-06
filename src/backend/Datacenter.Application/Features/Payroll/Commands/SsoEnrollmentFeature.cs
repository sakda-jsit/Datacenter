using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Payroll.Commands;

// ── สร้างรายการแจ้งเข้า/ออก ปกส. ───────────────────────────────────────────────
public record CreateSsoEnrollmentCommand(
    int ClientCompanyId, int EmployeeId, SsoEnrollmentType Type, DateTime EventDate, string? Note)
    : IRequest<int>, IRequireCompanyAccess;

public class CreateSsoEnrollmentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateSsoEnrollmentCommand, int>
{
    public async Task<int> Handle(CreateSsoEnrollmentCommand request, CancellationToken ct)
    {
        await UploadEmployeeDocumentCommandHandler.EnsureEmployee(db, request.EmployeeId, request.ClientCompanyId, ct);
        var enr = new SsoEnrollment
        {
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            EventDate = request.EventDate,
            Status = SsoEnrollmentStatus.Pending,
            Note = request.Note,
            CreatedBy = currentUser.Username,
        };
        db.SsoEnrollments.Add(enr);
        await db.SaveChangesAsync(ct);
        return enr.Id;
    }
}

// ── อัปเดต (แจ้งแล้ว + แนบหลักฐาน) → ปรับสถานะ ปกส. ของพนักงาน ──────────────────
public record UpdateSsoEnrollmentCommand(
    int ClientCompanyId, int Id, DateTime? SubmittedDate, SsoEnrollmentStatus Status,
    int? ProofDocumentId, string? Note)
    : IRequest<Unit>, IRequireCompanyAccess;

public class UpdateSsoEnrollmentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateSsoEnrollmentCommand, Unit>
{
    public async Task<Unit> Handle(UpdateSsoEnrollmentCommand request, CancellationToken ct)
    {
        var enr = await db.SsoEnrollments
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.Id == request.Id
                && e.Employee!.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("SsoEnrollment", request.Id);

        enr.SubmittedDate = request.SubmittedDate;
        enr.Status = request.Status;
        enr.ProofDocumentId = request.ProofDocumentId;
        enr.Note = request.Note;
        enr.ModifiedBy = currentUser.Username;
        enr.ModifiedAt = DateTime.UtcNow;

        // แจ้งแล้ว → ปรับสถานะผู้ประกันตนของพนักงาน
        if (request.Status == SsoEnrollmentStatus.Submitted && enr.Employee is { } emp)
            emp.SsoStatus = enr.Type == SsoEnrollmentType.Enroll
                ? SsoMemberStatus.Enrolled : SsoMemberStatus.Terminated;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
