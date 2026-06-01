using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar;

public static class ComplianceTaskHelpers
{
    public static string TaskTypeName(ComplianceTaskType t) => t switch
    {
        ComplianceTaskType.PP30          => "ภ.พ.30 (VAT)",
        ComplianceTaskType.PND1          => "ภ.ง.ด.1 (ภาษีหัก ณ ที่จ่าย พนักงาน)",
        ComplianceTaskType.PND3          => "ภ.ง.ด.3 (ภาษีหัก ณ ที่จ่าย บุคคลธรรมดา)",
        ComplianceTaskType.PND53         => "ภ.ง.ด.53 (ภาษีหัก ณ ที่จ่าย นิติบุคคล)",
        ComplianceTaskType.SSO           => "ประกันสังคม",
        ComplianceTaskType.MonthlyClosing => "ปิดบัญชีประจำเดือน",
        _                                => t.ToString(),
    };

    public static string StatusName(ComplianceTaskStatus s) => s switch
    {
        ComplianceTaskStatus.Pending    => "รอดำเนินการ",
        ComplianceTaskStatus.InProgress => "กำลังดำเนินการ",
        ComplianceTaskStatus.Completed  => "เสร็จสิ้น",
        ComplianceTaskStatus.Overdue    => "เกินกำหนด",
        _                               => s.ToString(),
    };
}
