using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// Computes statutory due dates for each compliance task type.
/// All dates follow Thai Revenue Department filing deadlines (e-filing).
/// </summary>
public static class ComplianceDueDateCalculator
{
    public static DateTime Calculate(ComplianceTaskType taskType, int year, int month)
    {
        return taskType switch
        {
            // PP30: 15th of the following month (e-filing)
            ComplianceTaskType.PP30 => NextMonthDay(year, month, 15),
            // PND1: 15th of the following month (e-filing)
            ComplianceTaskType.PND1 => NextMonthDay(year, month, 15),
            // PND3: 7th of the following month
            ComplianceTaskType.PND3 => NextMonthDay(year, month, 7),
            // PND53: 7th of the following month
            ComplianceTaskType.PND53 => NextMonthDay(year, month, 7),
            // SSO: 15th of the following month
            ComplianceTaskType.SSO => NextMonthDay(year, month, 15),
            // Monthly Closing: last day of following month
            ComplianceTaskType.MonthlyClosing => LastDayOfNextMonth(year, month),
            _ => throw new ArgumentOutOfRangeException(nameof(taskType)),
        };
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
