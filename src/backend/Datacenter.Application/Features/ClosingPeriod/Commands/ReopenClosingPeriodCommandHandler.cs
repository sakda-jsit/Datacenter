using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Commands;

public class ReopenClosingPeriodCommandHandler(
    IApplicationDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser)
    : IRequestHandler<ReopenClosingPeriodCommand, ClosingPeriodMonthDto>
{
    public async Task<ClosingPeriodMonthDto> Handle(ReopenClosingPeriodCommand request, CancellationToken ct)
    {
        // Business rule: "Reopen requires authorized user approval" — จำกัดเฉพาะ Admin
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("เฉพาะผู้ดูแลระบบ (Admin) เท่านั้นที่สามารถเปิดงวดที่ปิดแล้วได้");

        var period = await db.ClosingPeriods
            .FirstOrDefaultAsync(p => p.ClientCompanyId == request.ClientCompanyId
                                   && p.Year == request.Year
                                   && p.Month == request.Month, ct);

        if (period is null || period.Status == PeriodStatus.Open)
            throw new DomainException($"งวด {request.Year}/{request.Month:D2} ยังไม่ได้ปิด ไม่จำเป็นต้องเปิดใหม่");

        var previousStatus = period.Status;
        period.Status = PeriodStatus.Open;
        period.ClosedByUserId = null;
        period.ClosedAt = null;

        await audit.LogAsync(
            action: "ReopenPeriod",
            entityName: "ClosingPeriod",
            entityId: $"{request.ClientCompanyId}:{request.Year}/{request.Month:D2}",
            beforeValue: previousStatus.ToString(),
            afterValue: $"{PeriodStatus.Open}{(string.IsNullOrWhiteSpace(request.Reason) ? "" : $" — {request.Reason}")}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return CloseClosingPeriodCommandHandler.ToDto(period);
    }
}
