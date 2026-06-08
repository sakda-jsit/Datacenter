using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// รวมตรรกะ resolve template 2 ระดับ (global + เฉพาะบริษัท) ไว้ที่เดียว.
/// ลำดับความสำคัญ: company override → global → ค่าเริ่มต้น (เปิดทุกประเภท).
/// </summary>
public static class ComplianceTemplateResolver
{
    public static readonly ComplianceTaskType[] AllTypes = Enum.GetValues<ComplianceTaskType>();

    public record Effective(bool Enabled, int? DueDay, string Source);

    /// <summary>
    /// คืนสถานะ effective ต่อประเภทงานสำหรับบริษัทหนึ่ง.
    /// globalRules = แถวที่ ClientCompanyId == null, companyRules = แถวของบริษัทนั้น.
    /// </summary>
    public static IReadOnlyDictionary<ComplianceTaskType, Effective> Resolve(
        IEnumerable<ComplianceTaskTemplate> globalRules,
        IEnumerable<ComplianceTaskTemplate>? companyRules)
    {
        var g = globalRules.ToDictionary(r => r.TaskType);
        var c = companyRules?.ToDictionary(r => r.TaskType) ?? new();

        var result = new Dictionary<ComplianceTaskType, Effective>();
        foreach (var type in AllTypes)
        {
            if (c.TryGetValue(type, out var cr))
                result[type] = new Effective(cr.Enabled, cr.DueDay, "company");
            else if (g.TryGetValue(type, out var gr))
                result[type] = new Effective(gr.Enabled, gr.DueDay, "global");
            else
                result[type] = new Effective(true, null, "default"); // ค่าเริ่มต้น = เปิดทุกประเภท (คงพฤติกรรมเดิม)
        }
        return result;
    }
}
