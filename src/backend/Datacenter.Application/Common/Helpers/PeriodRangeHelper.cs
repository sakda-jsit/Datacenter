namespace Datacenter.Application.Common.Helpers;

public static class PeriodRangeHelper
{
    /// <summary>
    /// Returns (periodStart, periodEnd exclusive, yearStart) for a year + optional month range.
    /// Defaults to full year (Jan–Dec) when months are null.
    /// </summary>
    public static (DateTime PeriodStart, DateTime PeriodEnd, DateTime YearStart) Get(
        int year, int? monthFrom, int? monthTo)
    {
        int mFrom = monthFrom ?? 1;
        int mTo   = monthTo   ?? 12;
        return (
            new DateTime(year, mFrom, 1),
            new DateTime(year, mTo, DateTime.DaysInMonth(year, mTo)).AddDays(1),
            new DateTime(year, 1, 1)
        );
    }
}
