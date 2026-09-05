using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

public class GetComplianceTasksQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetComplianceTasksQuery, IReadOnlyList<ComplianceTaskDto>>
{
    public async Task<IReadOnlyList<ComplianceTaskDto>> Handle(GetComplianceTasksQuery request, CancellationToken ct)
    {
        var query = db.ComplianceTasks
            .Include(t => t.ClientCompany)
            .Include(t => t.AssignedUser)
            .Include(t => t.CompletedByUser)
            .Where(t => t.ClientCompanyId == request.ClientCompanyId && t.Year == request.Year);

        if (request.Month.HasValue)
            query = query.Where(t => t.Month == request.Month.Value);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        var tasks = await query.OrderBy(t => t.Month).ThenBy(t => t.TaskType).ToListAsync(ct);

        // จำนวนหลักฐานที่แนบกับแต่ละงาน (query เดียวสำหรับทุกงาน)
        var taskIds = tasks.Select(t => t.Id).ToList();
        var evidenceCounts = await db.Attachments
            .Where(a => a.ModuleName == ComplianceEvidence.ModuleName
                     && a.RecordId != null && taskIds.Contains(a.RecordId.Value))
            .GroupBy(a => a.RecordId!.Value)
            .Select(g => new { TaskId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TaskId, x => x.Count, ct);

        // กฎ "ต้องมีหลักฐาน" ต่อประเภทงาน (resolve template 2 ระดับ ครั้งเดียว)
        var globalRules = await db.ComplianceTaskTemplates.AsNoTracking()
            .Where(t => t.ClientCompanyId == null).ToListAsync(ct);
        var companyRules = await db.ComplianceTaskTemplates.AsNoTracking()
            .Where(t => t.ClientCompanyId == request.ClientCompanyId).ToListAsync(ct);
        var effective = ComplianceTemplateResolver.Resolve(globalRules, companyRules);

        var now = DateTime.UtcNow.Date;

        return tasks.Select(t => new ComplianceTaskDto(
            t.Id,
            t.ClientCompanyId,
            t.ClientCompany.Code,
            t.ClientCompany.LegalName,
            t.TaskType,
            ComplianceTaskHelpers.TaskTypeName(t.TaskType),
            t.Year,
            t.Month,
            t.DueDate,
            t.Status,
            ComplianceTaskHelpers.StatusName(t.Status),
            t.AssignedUserId,
            t.AssignedUser?.DisplayName,
            t.Note,
            t.CompletedAt,
            t.CompletedByUserId,
            t.CompletedByUser?.DisplayName,
            t.Status != ComplianceTaskStatus.Completed && t.DueDate.Date < now,
            evidenceCounts.GetValueOrDefault(t.Id),
            effective[t.TaskType].RequireEvidence
        )).ToList();
    }
}
