using Datacenter.Application.Features.ComplianceCalendar.Services;
using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar;

public static class ComplianceTaskHelpers
{
    public static string TaskTypeName(ComplianceTaskType t) => ComplianceTaskCatalog.Name(t);

    public static string StatusName(ComplianceTaskStatus s) => s switch
    {
        ComplianceTaskStatus.Pending    => "รอดำเนินการ",
        ComplianceTaskStatus.InProgress => "กำลังดำเนินการ",
        ComplianceTaskStatus.Completed  => "เสร็จสิ้น",
        ComplianceTaskStatus.Overdue    => "เกินกำหนด",
        _                               => s.ToString(),
    };
}
