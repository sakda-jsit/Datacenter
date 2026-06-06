using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// การแจ้งเข้า/ออกประกันสังคมของพนักงาน — ขับ checklist onboarding/offboarding
/// และเก็บหลักฐานการแจ้ง (เชื่อม EmployeeDocument).
/// </summary>
public class SsoEnrollment : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public SsoEnrollmentType Type { get; set; }
    /// <summary>วันที่เข้า/ออกงานจริง</summary>
    public DateTime EventDate { get; set; }
    /// <summary>วันที่แจ้งบนเว็บ ปกส. (null = ยังไม่แจ้ง)</summary>
    public DateTime? SubmittedDate { get; set; }
    public SsoEnrollmentStatus Status { get; set; } = SsoEnrollmentStatus.Pending;

    /// <summary>เอกสารหลักฐานการแจ้ง (อ้าง EmployeeDocument)</summary>
    public int? ProofDocumentId { get; set; }
    public string? Note { get; set; }
}
