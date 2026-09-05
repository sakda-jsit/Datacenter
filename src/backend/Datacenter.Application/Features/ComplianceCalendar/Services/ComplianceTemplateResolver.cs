using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// รวมตรรกะ resolve template 2 ระดับ (global + เฉพาะบริษัท) ไว้ที่เดียว.
/// ลำดับความสำคัญ: company override → global → ค่าเริ่มต้น (เปิดทุกประเภท).
/// </summary>
public static class ComplianceTemplateResolver
{
    /// <summary>ทุกประเภทงาน เรียงตามรอบ (รายเดือน → ครึ่งปี → รายปี) ตามลำดับในทะเบียนกลาง</summary>
    public static readonly ComplianceTaskType[] AllTypes =
        ComplianceTaskCatalog.All.Select(e => e.Type).ToArray();

    public record Effective(bool Enabled, int? DueDay, int? DueMonthsAfter, bool RequireEvidence, string Source);

    /// <summary>
    /// ค่าเริ่มต้นของ "ต้องแนบหลักฐาน" ต่อประเภทงาน — งานที่ยื่นแบบกับราชการต้องมีหลักฐานการยื่น
    /// ส่วนงานปิดบัญชีประจำเดือนเป็นงานภายใน ไม่มีใบเสร็จให้แนบ
    /// </summary>
    public static bool DefaultRequireEvidence(ComplianceTaskType type)
        => ComplianceTaskCatalog.Get(type).RequireEvidence;

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
            bool fallbackEvidence = DefaultRequireEvidence(type);
            if (c.TryGetValue(type, out var cr))
                result[type] = new Effective(cr.Enabled, cr.DueDay, cr.DueMonthsAfter, cr.RequireEvidence ?? fallbackEvidence, "company");
            else if (g.TryGetValue(type, out var gr))
                result[type] = new Effective(gr.Enabled, gr.DueDay, gr.DueMonthsAfter, gr.RequireEvidence ?? fallbackEvidence, "global");
            else
                result[type] = new Effective(true, null, null, fallbackEvidence, "default"); // ค่าเริ่มต้น = เปิดทุกประเภท (คงพฤติกรรมเดิม)
        }
        return result;
    }
}
