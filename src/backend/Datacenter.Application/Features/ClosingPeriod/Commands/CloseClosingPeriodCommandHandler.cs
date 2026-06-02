using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Datacenter.Application.Features.ClosingPeriod.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Commands;

public class CloseClosingPeriodCommandHandler(
    IApplicationDbContext db,
    IAuditService audit,
    ICurrentUserService currentUser)
    : IRequestHandler<CloseClosingPeriodCommand, ClosingPeriodMonthDto>
{
    public async Task<ClosingPeriodMonthDto> Handle(CloseClosingPeriodCommand request, CancellationToken ct)
    {
        var period = await db.ClosingPeriods
            .FirstOrDefaultAsync(p => p.ClientCompanyId == request.ClientCompanyId
                                   && p.Year == request.Year
                                   && p.Month == request.Month, ct);

        if (period is { Status: PeriodStatus.Closed or PeriodStatus.Locked })
            throw new DomainException($"งวด {request.Year}/{request.Month:D2} ถูกปิดแล้ว");

        // บังคับตรวจความพร้อมก่อนปิดจริง (ไม่พึ่ง preview จาก client)
        var items = await ClosingValidationService.ValidateAsync(
            db, request.ClientCompanyId, request.Year, request.Month, ct);

        if (!ClosingValidationService.CanClose(items))
        {
            var failed = items.First(i => i.Severity == "Error" && !i.Passed);
            throw new DomainException($"ไม่สามารถปิดงวดได้: {failed.Label} — {failed.Detail}");
        }

        if (period is null)
        {
            period = new Domain.Entities.ClosingPeriod
            {
                ClientCompanyId = request.ClientCompanyId,
                Year = request.Year,
                Month = request.Month,
            };
            db.ClosingPeriods.Add(period);
        }

        var previousStatus = period.Status;
        period.Status = PeriodStatus.Closed;
        period.ClosedByUserId = currentUser.UserId;
        period.ClosedAt = DateTime.UtcNow;

        await audit.LogAsync(
            action: "ClosePeriod",
            entityName: "ClosingPeriod",
            entityId: $"{request.ClientCompanyId}:{request.Year}/{request.Month:D2}",
            beforeValue: previousStatus.ToString(),
            afterValue: PeriodStatus.Closed.ToString(),
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);

        return ToDto(period);
    }

    internal static ClosingPeriodMonthDto ToDto(Domain.Entities.ClosingPeriod p, string? closedByName = null)
        => new(p.Year, p.Month, p.Status, StatusName(p.Status), p.ClosedAt, p.ClosedByUserId, closedByName,
               BeginDate: null, EndDate: null, SourceLocked: false);

    internal static string StatusName(PeriodStatus status) => status switch
    {
        PeriodStatus.Open => "เปิดอยู่",
        PeriodStatus.Closed => "ปิดงวดแล้ว",
        PeriodStatus.Locked => "ล็อกถาวร",
        _ => status.ToString(),
    };
}
