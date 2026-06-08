using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// แม่แบบงานประจำ (ปฏิทินงาน) 2 ระดับ:
/// - ClientCompanyId = null → ระดับ 1 "ทุกบริษัท" (ค่าเริ่มต้นกลาง)
/// - ClientCompanyId = X    → ระดับ 2 "เฉพาะบริษัท" (ทับค่า global รายประเภท)
/// กำหนดว่างานประเภทไหน "เปิด" ตอน generate งานรายเดือน + วันครบกำหนด (override ได้).
/// </summary>
public class ComplianceTaskTemplate : BaseEntity
{
    public int? ClientCompanyId { get; set; }   // null = global
    public ComplianceTaskType TaskType { get; set; }
    public bool Enabled { get; set; } = true;
    public int? DueDay { get; set; }             // null = ใช้ค่า default ของ calculator (0/น้อยกว่า = สิ้นเดือนถัดไป)

    public ClientCompany? ClientCompany { get; set; }
}
