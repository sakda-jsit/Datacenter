using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// คำนวณวันครบกำหนดของงานในปฏิทินงาน โดยนับจาก "วันสิ้นงวด" เสมอ
/// (งานรายเดือน = สิ้นเดือนนั้น, ครึ่งปี/รายปี = สิ้นงวดของรอบบัญชี)
/// กติกาเริ่มต้นรายประเภทอยู่ใน <see cref="ComplianceTaskCatalog"/>
/// </summary>
public static class ComplianceDueDateCalculator
{
    /// <summary>วันครบกำหนดเริ่มต้น (วันของเดือนเป้าหมาย; 0 = วันสุดท้ายของเดือนนั้น)</summary>
    public static int DefaultDueDay(ComplianceTaskType taskType) => ComplianceTaskCatalog.Get(taskType).Due.Day;

    /// <summary>ครบกำหนดกี่เดือนหลังสิ้นงวด (ค่าเริ่มต้นของประเภทงาน)</summary>
    public static int DefaultDueMonthsAfter(ComplianceTaskType taskType) => ComplianceTaskCatalog.Get(taskType).Due.MonthsAfter;

    /// <summary>
    /// คำนวณวันครบกำหนดของงานงวด (year, month).
    /// month = เดือนที่งวดสิ้นสุด — รายเดือนคือเดือนนั้น, ครึ่งปี/รายปีคือเดือนสุดท้ายของงวด.
    /// overrideDay / overrideMonthsAfter = ค่าที่ตั้งทับไว้ใน template (null = ใช้ค่าเริ่มต้น)
    /// </summary>
    public static DateTime Calculate(
        ComplianceTaskType taskType, int year, int month,
        int? overrideDay = null, int? overrideMonthsAfter = null)
    {
        var rule = ComplianceTaskCatalog.Get(taskType).Due;
        var periodEnd = LastDayOf(year, month);

        // นับเป็นจำนวนวัน (เช่น ภ.ง.ด.50 = 150 วัน) — ตั้งทับด้วย template ไม่ได้ เพราะเป็นกติกาตามกฎหมาย
        if (rule.DaysAfter > 0 && overrideDay is null && overrideMonthsAfter is null)
            return periodEnd.AddDays(rule.DaysAfter);

        int monthsAfter = overrideMonthsAfter ?? rule.MonthsAfter;
        int day = overrideDay ?? rule.Day;

        var target = periodEnd.AddMonths(Math.Max(0, monthsAfter));
        return day <= 0
            ? LastDayOf(target.Year, target.Month)
            : new DateTime(target.Year, target.Month, Math.Min(day, DateTime.DaysInMonth(target.Year, target.Month)));
    }

    private static DateTime LastDayOf(int year, int month)
        => new(year, month, DateTime.DaysInMonth(year, month));
}
