namespace Datacenter.Domain.Enums;

public enum ComplianceTaskType
{
    PP30 = 1,       // ภาษีมูลค่าเพิ่ม
    PND1 = 2,       // ภาษีหัก ณ ที่จ่าย พนักงาน
    PND3 = 3,       // ภาษีหัก ณ ที่จ่าย บุคคลธรรมดา
    PND53 = 4,      // ภาษีหัก ณ ที่จ่าย นิติบุคคล
    SSO = 5,        // ประกันสังคม
    MonthlyClosing = 6, // ปิดบัญชีประจำเดือน
}
