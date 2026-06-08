using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.ComplianceCalendar.DTOs;
using Datacenter.Application.Features.ComplianceCalendar.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.ComplianceCalendar.Queries;

/// <summary>
/// คืน template งานประจำ 6 ประเภท (สถานะ effective).
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
        if (companyId is int cid)
            companyRules = await db.ComplianceTaskTemplates.AsNoTracking()
                .Where(t => t.ClientCompanyId == cid).ToListAsync(ct);

        if (companyId is null)
        {
            // ระดับ global: แสดงค่าที่ตั้งไว้ หรือค่าเริ่มต้น
            var gmap = globalRules.ToDictionary(r => r.TaskType);
            return ComplianceTemplateResolver.AllTypes.Select(type =>
            {
                gmap.TryGetValue(type, out var gr);
                return new ComplianceTaskTemplateDto(
                    type, ComplianceTaskHelpers.TaskTypeName(type),
                    gr?.Enabled ?? true, gr?.DueDay,
                    ComplianceDueDateCalculator.DefaultDueDay(type),
                    gr is null ? "default" : "global");
            }).ToList();
        }

        // ระดับเฉพาะบริษัท: ใช้ resolver (company > global > default)
        var eff = ComplianceTemplateResolver.Resolve(globalRules, companyRules);
        return ComplianceTemplateResolver.AllTypes.Select(type =>
        {
            var e = eff[type];
            return new ComplianceTaskTemplateDto(
                type, ComplianceTaskHelpers.TaskTypeName(type),
                e.Enabled, e.DueDay,
                ComplianceDueDateCalculator.DefaultDueDay(type),
                e.Source);
        }).ToList();
    }
}
