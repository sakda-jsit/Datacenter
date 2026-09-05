using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// สร้างงานในปฏิทินงานของบริษัทหนึ่งสำหรับงวด (ปี/เดือน) ตาม template ที่ตั้งไว้.
/// ใช้ร่วมกันระหว่างการกดสร้างเองในหน้าจอ กับงานเบื้องหลังที่สร้างให้อัตโนมัติทุกวัน
/// — ตรรกะจึงอยู่ที่เดียว ไม่มีทางเพี้ยนกัน.
/// <para><b>Idempotent</b> — งานที่มีอยู่แล้วจะถูกข้าม เรียกซ้ำกี่ครั้งก็ไม่เกิดงานซ้ำ</para>
/// </summary>
public static class ComplianceTaskGenerator
{
    /// <summary>
    /// เพิ่มงานที่ยังขาดของบริษัทนี้ลงใน change tracker (ยังไม่ SaveChanges — ผู้เรียกเป็นคนบันทึก)
    /// </summary>
    public static async Task<IReadOnlyList<ComplianceTask>> BuildMissingAsync(
        IApplicationDbContext db, int clientCompanyId, int year, int month, CancellationToken ct)
    {
        var existing = await db.ComplianceTasks
            .Where(t => t.ClientCompanyId == clientCompanyId && t.Year == year && t.Month == month)
            .Select(t => t.TaskType)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        // resolve template 2 ระดับ (เฉพาะบริษัท > global > ค่าเริ่มต้น) → สร้างเฉพาะประเภทที่ "เปิด"
        var globalRules = await db.ComplianceTaskTemplates.AsNoTracking()
            .Where(t => t.ClientCompanyId == null).ToListAsync(ct);
        var companyRules = await db.ComplianceTaskTemplates.AsNoTracking()
            .Where(t => t.ClientCompanyId == clientCompanyId).ToListAsync(ct);
        var effective = ComplianceTemplateResolver.Resolve(globalRules, companyRules);

        // ผู้รับผิดชอบประจำบริษัท + เดือนเริ่มรอบบัญชี (กำหนดว่างานครึ่งปี/รายปีตกเดือนไหน)
        var company = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.Id == clientCompanyId)
            .Select(c => new { c.DefaultAssigneeUserId, c.FiscalYearStartMonth })
            .FirstOrDefaultAsync(ct);
        if (company is null)
            return [];

        int fiscalStart = company.FiscalYearStartMonth;

        // งานรายเดือนสร้างทุกเดือน ส่วนงานครึ่งปี/รายปีสร้างเฉพาะเดือนที่งวดนั้นสิ้นสุด
        return ComplianceTemplateResolver.AllTypes
            .Where(type => !existingSet.Contains(type)
                        && effective[type].Enabled
                        && ComplianceTaskCatalog.OccursIn(type, month, fiscalStart))
            .Select(type => new ComplianceTask
            {
                ClientCompanyId = clientCompanyId,
                TaskType = type,
                Year = year,
                Month = month,
                DueDate = ComplianceDueDateCalculator.Calculate(
                    type, year, month, effective[type].DueDay, effective[type].DueMonthsAfter),
                Status = ComplianceTaskStatus.Pending,
                AssignedUserId = company.DefaultAssigneeUserId,
            })
            .ToList();
    }
}
