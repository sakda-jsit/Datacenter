using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// Computes statutory due dates for each compliance task type.
/// All dates follow Thai Revenue Department filing deadlines (e-filing).
/// </summary>
public static class ComplianceDueDateCalculator
{
    /// <summary>วันครบกำหนดเริ่มต้นต่อประเภทงาน (วันของเดือนถัดไป; 0 = วันสุดท้ายของเดือนถัดไป)</summary>
    public static int DefaultDueDay(ComplianceTaskType taskType) => taskType switch
    {
        ComplianceTaskType.PP30 => 15,
        ComplianceTaskType.PND1 => 15,
        ComplianceTaskType.PND3 => 7,
        ComplianceTaskType.PND53 => 7,
        ComplianceTaskType.SSO => 15,
        ComplianceTaskType.MonthlyClosing => 0, // 0 = วันสุดท้ายของเดือนถัดไป
        _ => 15,
    };

    /// <summary>
    /// คำนวณวันครบกำหนด. overrideDay = วันของเดือนถัดไปที่ต้องการ (null = ใช้ค่า default;
    /// ค่า ≤ 0 = วันสุดท้ายของเดือนถัดไป).
    /// </summary>
    public static DateTime Calculate(ComplianceTaskType taskType, int year, int month, int? overrideDay = null)
    {
        int day = overrideDay ?? DefaultDueDay(taskType);
        return day <= 0 ? LastDayOfNextMonth(year, month) : NextMonthDay(year, month, day);
    }

    private static DateTime NextMonthDay(int year, int month, int day)
    {
        var next = new DateTime(year, month, 1).AddMonths(1);
        int actualDay = Math.Min(day, DateTime.DaysInMonth(next.Year, next.Month));
        return new DateTime(next.Year, next.Month, actualDay);
    }

    private static DateTime LastDayOfNextMonth(int year, int month)
    {
        var next = new DateTime(year, month, 1).AddMonths(1);
        return new DateTime(next.Year, next.Month, DateTime.DaysInMonth(next.Year, next.Month));
    }
}
