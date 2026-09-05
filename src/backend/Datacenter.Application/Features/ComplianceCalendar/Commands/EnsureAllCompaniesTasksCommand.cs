using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

/// <summary>
/// สร้างงานของงวด (ปี/เดือน) ให้ <b>ทุกบริษัทที่ยังใช้งานอยู่</b> ตาม template ที่ตั้งไว้.
/// ใช้โดยงานเบื้องหลังรายวัน และปุ่ม "สร้างงานให้ทุกบริษัท" ของผู้ดูแลระบบ.
///
/// ไม่ใช่ IRequireCompanyAccess เพราะทำข้ามทุกบริษัท — สิทธิ์คุมที่ controller (Admin เท่านั้น)
/// ส่วนงานเบื้องหลังไม่มีผู้ใช้ จึงบันทึก audit เป็น "system"
/// </summary>
public record EnsureAllCompaniesTasksCommand(int Year, int Month, string TriggeredBy)
    : IRequest<EnsureAllCompaniesTasksResult>;

/// <param name="CompaniesTouched">จำนวนบริษัทที่มีงานถูกสร้างเพิ่มจริง</param>
public record EnsureAllCompaniesTasksResult(int Year, int Month, int Created, int CompaniesTouched, int CompaniesChecked);

public class EnsureAllCompaniesTasksCommandHandler(IApplicationDbContext db)
    : IRequestHandler<EnsureAllCompaniesTasksCommand, EnsureAllCompaniesTasksResult>
{
    public async Task<EnsureAllCompaniesTasksResult> Handle(EnsureAllCompaniesTasksCommand request, CancellationToken ct)
    {
        var companyIds = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(ct);

        int created = 0, touched = 0;
        foreach (var id in companyIds)
        {
            var toCreate = await ComplianceTaskGenerator.BuildMissingAsync(db, id, request.Year, request.Month, ct);
            if (toCreate.Count == 0)
                continue;

            db.ComplianceTasks.AddRange(toCreate);
            created += toCreate.Count;
            touched++;
        }

        if (created > 0)
        {
            // สรุปรวมแถวเดียว — ไม่งั้น audit log จะถูกถล่มด้วยแถวรายบริษัททุกวัน
            db.AuditLogs.Add(new Domain.Entities.AuditLog
            {
                Username = request.TriggeredBy,
                Action = "GenerateTasksAllCompanies",
                EntityName = "ComplianceTask",
                EntityId = $"{request.Year}/{request.Month:D2}",
                AfterValue = $"{created} tasks created for {touched} companies",
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        return new EnsureAllCompaniesTasksResult(request.Year, request.Month, created, touched, companyIds.Count);
    }
}
