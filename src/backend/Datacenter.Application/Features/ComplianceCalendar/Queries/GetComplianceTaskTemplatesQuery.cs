using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

/// <summary>
/// คืน template งานประจำทุกประเภท (สถานะ effective) เรียงตามรอบ รายเดือน → ครึ่งปี → รายปี.
/// ClientCompanyId = null/0 → ระดับ global (ทุกบริษัท); >0 → เฉพาะบริษัท (แสดง override + ที่ inherit).
/// </summary>
public record GetComplianceTaskTemplatesQuery(int? ClientCompanyId)
    : IRequest<IReadOnlyList<ComplianceTaskTemplateDto>>;

public class GetComplianceTaskTemplatesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetComplianceTaskTemplatesQuery, IReadOnlyList<ComplianceTaskTemplateDto>>
{
    public async Task<IReadOnlyList<ComplianceTaskTemplateDto>> Handle(GetComplianceTaskTemplatesQuery request, CancellationToken ct)
    {
        int? companyId = request.ClientCompanyId is > 0 ? request.ClientCompanyId : null;

        var globalRules = await db.ComplianceTaskTemplates.AsNoTracking()
            .Where(t => t.ClientCompanyId == null).ToListAsync(ct);

        List<Domain.Entities.ComplianceTaskTemplate>? companyRules = null;
        int fiscalStart = 1;
        if (companyId is int cid)
        {
            companyRules = await db.ComplianceTaskTemplates.AsNoTracking()
                .Where(t => t.ClientCompanyId == cid).ToListAsync(ct);
            fiscalStart = await db.ClientCompanies.AsNoTracking()
                .Where(c => c.Id == cid).Select(c => c.FiscalYearStartMonth).FirstOrDefaultAsync(ct);
            if (fiscalStart is < 1 or > 12) fiscalStart = 1;
        }

        if (companyId is null)
        {
            // ระดับ global: แสดงค่าที่ตั้งไว้ หรือค่าเริ่มต้น
            var gmap = globalRules.ToDictionary(r => r.TaskType);
            return ComplianceTemplateResolver.AllTypes.Select(type =>
            {
                gmap.TryGetValue(type, out var gr);
                return Build(type, gr?.Enabled ?? true, gr?.DueDay, gr?.DueMonthsAfter,
                    gr?.RequireEvidence ?? ComplianceTemplateResolver.DefaultRequireEvidence(type),
                    gr is null ? "default" : "global", fiscalStart);
            }).ToList();
        }

        // ระดับเฉพาะบริษัท: ใช้ resolver (company > global > default)
        var eff = ComplianceTemplateResolver.Resolve(globalRules, companyRules);
        return ComplianceTemplateResolver.AllTypes.Select(type =>
        {
            var e = eff[type];
            return Build(type, e.Enabled, e.DueDay, e.DueMonthsAfter, e.RequireEvidence, e.Source, fiscalStart);
        }).ToList();
    }

    private static ComplianceTaskTemplateDto Build(
        Domain.Enums.ComplianceTaskType type, bool enabled, int? dueDay, int? dueMonthsAfter,
        bool requireEvidence, string source, int fiscalStart)
    {
        var cycle = ComplianceTaskCatalog.Cycle(type);

        // งวดตัวอย่างของปีนี้ — งานรายเดือนใช้เดือนปัจจุบัน, งานรอบยาวใช้เดือนที่งวดสิ้นสุดจริง
        int sampleYear = DateTime.Now.Year;
        int sampleMonth = ComplianceTaskCatalog.PeriodEndMonth(type, fiscalStart) ?? DateTime.Now.Month;
        return new ComplianceTaskTemplateDto(
            type,
            ComplianceTaskHelpers.TaskTypeName(type),
            cycle,
            ComplianceTaskCatalog.CycleName(cycle),
            enabled,
            dueDay,
            ComplianceDueDateCalculator.DefaultDueDay(type),
            dueMonthsAfter,
            ComplianceDueDateCalculator.DefaultDueMonthsAfter(type),
            ComplianceTaskCatalog.DueDescription(type, dueDay, dueMonthsAfter),
            ComplianceTaskCatalog.UsesDaysAfterRule(type, dueDay, dueMonthsAfter),
            ComplianceTaskCatalog.PeriodLabel(type, sampleYear, sampleMonth),
            ComplianceDueDateCalculator.Calculate(type, sampleYear, sampleMonth, dueDay, dueMonthsAfter),
            requireEvidence,
            ComplianceTemplateResolver.DefaultRequireEvidence(type),
            source);
    }
}
