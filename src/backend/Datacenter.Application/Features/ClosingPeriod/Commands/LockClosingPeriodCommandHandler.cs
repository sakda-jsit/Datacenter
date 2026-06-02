using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Commands;

public class LockClosingPeriodCommandHandler(
    IApplicationDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser)
    : IRequestHandler<LockClosingPeriodCommand, ClosingPeriodMonthDto>
{
    public async Task<ClosingPeriodMonthDto> Handle(LockClosingPeriodCommand request, CancellationToken ct)
    {
        // ล็อกถาวรเป็นการกระทำที่ย้อนกลับยาก จำกัดเฉพาะ Admin เช่นเดียวกับการเปิดงวด
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("เฉพาะผู้ดูแลระบบ (Admin) เท่านั้นที่สามารถล็อกงวดถาวรได้");

        var period = await db.ClosingPeriods
            .FirstOrDefaultAsync(p => p.ClientCompanyId == request.ClientCompanyId
                                   && p.Year == request.Year
                                   && p.Month == request.Month, ct);

        if (period is null || period.Status == PeriodStatus.Open)
            throw new DomainException($"งวด {request.Year}/{request.Month:D2} ต้องปิดงวดก่อนจึงจะล็อกถาวรได้");

        if (period.Status == PeriodStatus.Locked)
            throw new DomainException($"งวด {request.Year}/{request.Month:D2} ถูกล็อกถาวรอยู่แล้ว");

        var previousStatus = period.Status;
        period.Status = PeriodStatus.Locked;

        await audit.LogAsync(
            action: "LockPeriod",
            entityName: "ClosingPeriod",
            entityId: $"{request.ClientCompanyId}:{request.Year}/{request.Month:D2}",
            beforeValue: previousStatus.ToString(),
            afterValue: PeriodStatus.Locked.ToString(),
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return CloseClosingPeriodCommandHandler.ToDto(period);
    }
}
