using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Queries;

/// <summary>
/// ภาระงานรายคน — รวมงานที่ยังไม่เสร็จของทุกบริษัทที่ผู้ใช้ปัจจุบันเข้าถึงได้ แล้วนับแยกตามผู้รับผิดชอบ.
/// ตอบคำถาม "ใครค้างงานกี่ชิ้น เกินกำหนดกี่ชิ้น" ในหน้าจอเดียว (แถวสุดท้าย = งานที่ยังไม่มอบหมาย).
/// ไม่ใช่ IRequireCompanyAccess (aggregate หลายบริษัท → กรองด้วย ICompanyAccessGuard เหมือน GetWorkboardQuery).
/// </summary>
public record GetWorkloadByUserQuery(
    bool IncludeCompliance,   // รวมงานภาษีจากปฏิทินงานด้วยหรือไม่
    int DueSoonDays = 7)      // ช่วง "ใกล้ครบกำหนด" (วัน)
    : IRequest<IReadOnlyList<UserWorkloadDto>>;

public class GetWorkloadByUserQueryHandler(IApplicationDbContext db, ICompanyAccessGuard guard)
    : IRequestHandler<GetWorkloadByUserQuery, IReadOnlyList<UserWorkloadDto>>
{
    public async Task<IReadOnlyList<UserWorkloadDto>> Handle(GetWorkloadByUserQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var soonLimit = today.AddDays(request.DueSoonDays <= 0 ? 7 : request.DueSoonDays);
        var accessible = await guard.GetAccessibleCompanyIdsAsync(ct); // null = admin (ทุกบริษัท)

        // (ผู้รับผิดชอบ, ชื่อ, บริษัท, กำหนดส่ง, เกินกำหนด)
        var rows = new List<(int? UserId, string? Name, int CompanyId, DateTime? Due, bool Overdue)>();

        // ── งานทั่วไป (WorkTask) ที่ยังไม่เสร็จ/ยังไม่ยกเลิก ──
        var taskQ = db.WorkTasks.AsNoTracking().Include(t => t.AssignedUser)
            .Where(t => t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled);
        if (accessible is not null) taskQ = taskQ.Where(t => accessible.Contains(t.ClientCompanyId));

        foreach (var t in await taskQ.ToListAsync(ct))
            rows.Add((t.AssignedUserId, t.AssignedUser?.DisplayName, t.ClientCompanyId, t.DueDate,
                      t.DueDate.HasValue && t.DueDate.Value.Date < today));

        // ── งานภาษี (ComplianceTask) ที่ยังไม่เสร็จ ──
        if (request.IncludeCompliance)
        {
            var compQ = db.ComplianceTasks.AsNoTracking().Include(t => t.AssignedUser)
                .Where(t => t.Status != ComplianceTaskStatus.Completed);
            if (accessible is not null) compQ = compQ.Where(t => accessible.Contains(t.ClientCompanyId));

            foreach (var t in await compQ.ToListAsync(ct))
                rows.Add((t.AssignedUserId, t.AssignedUser?.DisplayName, t.ClientCompanyId, t.DueDate,
                          t.Status == ComplianceTaskStatus.Overdue || t.DueDate.Date < today));
        }

        return rows
            .GroupBy(r => r.UserId)
            .Select(g => new UserWorkloadDto(
                UserId: g.Key,
                DisplayName: g.Key is null ? "— ยังไม่มอบหมาย —" : (g.First().Name ?? $"ผู้ใช้ #{g.Key}"),
                OpenCount: g.Count(),
                OverdueCount: g.Count(r => r.Overdue),
                DueSoonCount: g.Count(r => !r.Overdue && r.Due.HasValue && r.Due.Value.Date <= soonLimit),
                NoDueDateCount: g.Count(r => r.Due is null),
                CompanyCount: g.Select(r => r.CompanyId).Distinct().Count(),
                EarliestDueDate: g.Where(r => r.Due.HasValue).Select(r => r.Due!.Value).DefaultIfEmpty().Min() is var min
                                 && min == default ? null : min))
            // เกินกำหนดมากสุดขึ้นก่อน → งานค้างมากสุด → แถว "ยังไม่มอบหมาย" ไว้ท้ายสุด
            .OrderBy(x => x.UserId is null)
            .ThenByDescending(x => x.OverdueCount)
            .ThenByDescending(x => x.OpenCount)
            .ToList();
    }
}
