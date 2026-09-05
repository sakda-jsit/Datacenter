using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Commands;

public class GenerateMonthlyTasksCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<GenerateMonthlyTasksCommand, int>
{
    public async Task<int> Handle(GenerateMonthlyTasksCommand request, CancellationToken ct)
    {
        var existing = await db.ComplianceTasks
            .Where(t => t.ClientCompanyId == request.ClientCompanyId
                     && t.Year == request.Year
                     && t.Month == request.Month)
            .Select(t => t.TaskType)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        // resolve template 2 ระดับ (เฉพาะบริษัท > global > ค่าเริ่มต้น) → สร้างเฉพาะประเภทที่ "เปิด"
        var globalRules = await db.ComplianceTaskTemplates
            .Where(t => t.ClientCompanyId == null).ToListAsync(ct);
        var companyRules = await db.ComplianceTaskTemplates
            .Where(t => t.ClientCompanyId == request.ClientCompanyId).ToListAsync(ct);
        var effective = ComplianceTemplateResolver.Resolve(globalRules, companyRules);

        // ผู้รับผิดชอบประจำบริษัท + เดือนเริ่มรอบบัญชี (กำหนดว่างานครึ่งปี/รายปีตกเดือนไหน)
        var company = await db.ClientCompanies
            .Where(c => c.Id == request.ClientCompanyId)
            .Select(c => new { c.DefaultAssigneeUserId, c.FiscalYearStartMonth })
            .FirstOrDefaultAsync(ct);
        var defaultAssigneeId = company?.DefaultAssigneeUserId;
        int fiscalStart = company?.FiscalYearStartMonth ?? 1;

        // งานรายเดือนสร้างทุกเดือน ส่วนงานครึ่งปี/รายปีสร้างเฉพาะเดือนที่งวดนั้นสิ้นสุด
        var toCreate = ComplianceTemplateResolver.AllTypes
            .Where(type => !existingSet.Contains(type)
                        && effective[type].Enabled
                        && ComplianceTaskCatalog.OccursIn(type, request.Month, fiscalStart))
            .Select(type => new ComplianceTask
            {
                ClientCompanyId = request.ClientCompanyId,
                TaskType = type,
                Year = request.Year,
                Month = request.Month,
                DueDate = ComplianceDueDateCalculator.Calculate(
                    type, request.Year, request.Month, effective[type].DueDay, effective[type].DueMonthsAfter),
                Status = ComplianceTaskStatus.Pending,
                AssignedUserId = defaultAssigneeId,
            })
            .ToList();

        if (toCreate.Count == 0)
            return 0;

        db.ComplianceTasks.AddRange(toCreate);

        await audit.LogAsync("GenerateTasks", "ComplianceTask",
            $"{request.ClientCompanyId}:{request.Year}/{request.Month:D2}",
            afterValue: $"{toCreate.Count} tasks created",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);

        await db.SaveChangesAsync(ct);
        return toCreate.Count;
    }
}
