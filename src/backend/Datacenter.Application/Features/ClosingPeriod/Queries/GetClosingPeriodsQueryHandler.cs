using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ClosingPeriod.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ClosingPeriod.Queries;

public class GetClosingPeriodsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetClosingPeriodsQuery, ClosingPeriodOverviewDto>
{
    public async Task<ClosingPeriodOverviewDto> Handle(GetClosingPeriodsQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        // นิยามรอบบัญชี (จาก Express ISPRD) เป็นตัวกำหนดว่ามีงวดใดบ้างในปีนี้
        var definition = await db.AccountingPeriods
            .AsNoTracking()
            .Where(p => p.ClientCompanyId == request.ClientCompanyId && p.Year == request.Year)
            .OrderBy(p => p.PeriodNo)
            .ToListAsync(ct);

        var closing = await db.ClosingPeriods
            .AsNoTracking()
            .Where(p => p.ClientCompanyId == request.ClientCompanyId && p.Year == request.Year)
            .ToListAsync(ct);
        var closingByMonth = closing.ToDictionary(p => p.Month);

        // ดึงชื่อผู้ปิดงวดในครั้งเดียว เลี่ยง N+1
        var closerIds = closing.Where(p => p.ClosedByUserId.HasValue)
            .Select(p => p.ClosedByUserId!.Value).Distinct().ToList();
        var closerNames = await db.Users
            .AsNoTracking()
            .Where(u => closerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        bool isDefined = definition.Count > 0;

        ClosingPeriodMonthDto Build(int month, DateTime? begin, DateTime? end, bool sourceLocked)
        {
            if (closingByMonth.TryGetValue(month, out var c))
            {
                string? closerName = c.ClosedByUserId.HasValue && closerNames.TryGetValue(c.ClosedByUserId.Value, out var n)
                    ? n : null;
                return new ClosingPeriodMonthDto(
                    request.Year, month, c.Status, StatusName(c.Status),
                    c.ClosedAt, c.ClosedByUserId, closerName, begin, end, sourceLocked);
            }
            return new ClosingPeriodMonthDto(
                request.Year, month, PeriodStatus.Open, StatusName(PeriodStatus.Open),
                null, null, null, begin, end, sourceLocked);
        }

        var months = isDefined
            ? definition.Select(p => Build(p.PeriodNo, p.BeginDate, p.EndDate, p.SourceLocked)).ToList()
            : [];

        return new ClosingPeriodOverviewDto(
            client.Id, client.Code, client.Name, request.Year, isDefined, months);
    }

    private static string StatusName(PeriodStatus status) => status switch
    {
        PeriodStatus.Open => "เปิดอยู่",
        PeriodStatus.Closed => "ปิดงวดแล้ว",
        PeriodStatus.Locked => "ล็อกถาวร",
        _ => status.ToString(),
    };
}
