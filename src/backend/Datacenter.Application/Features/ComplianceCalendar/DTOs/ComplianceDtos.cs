using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.DTOs;

public record ComplianceTaskDto(
    int Id,
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    ComplianceTaskType TaskType,
    string TaskTypeName,
    /// <summary>รอบของงาน — รายเดือน / ครึ่งปี / รายปี</summary>
    ComplianceCycle Cycle,
    string CycleName,
    /// <summary>คำอธิบายงวด เช่น "ม.ค. 2026", "ครึ่งปีแรก 2026", "ปีบัญชี 2026"</summary>
    string PeriodLabel,
    int Year,
    int Month,
    DateTime DueDate,
    ComplianceTaskStatus Status,
    string StatusName,
    int? AssignedUserId,
    string? AssignedUserName,
    string? Note,
    DateTime? CompletedAt,
    int? CompletedByUserId,
    string? CompletedByUserName,
    bool IsOverdue,
    /// <summary>จำนวนหลักฐาน (แบบที่ยื่น/ใบเสร็จ) ที่แนบกับงานงวดนี้</summary>
    int EvidenceCount,
    /// <summary>งานประเภทนี้ต้องมีหลักฐานก่อนปิดเป็น "เสร็จสิ้น" หรือไม่</summary>
    bool RequireEvidence
);

public record MonthSummaryDto(
    int Month,
    int Total,
    int Completed,
    int InProgress,
    int Pending,
    int Overdue
);

/// <summary>หนึ่งประเภทงานใน template (ระดับ global หรือเฉพาะบริษัท)</summary>
public record ComplianceTaskTemplateDto(
    ComplianceTaskType TaskType,
    string TaskTypeName,
    ComplianceCycle Cycle,       // รอบของงาน (กำหนดตายตัวตามประเภท แก้ไม่ได้)
    string CycleName,
    bool Enabled,
    int? DueDay,          // วันของเดือนเป้าหมาย (override); null = ใช้ค่าเริ่มต้น, 0 = วันสุดท้ายของเดือน
    int DefaultDueDay,    // ค่าเริ่มต้นของประเภทนี้
    int? DueMonthsAfter,  // ครบกำหนดกี่เดือนหลังสิ้นงวด (override); null = ใช้ค่าเริ่มต้น
    int DefaultDueMonthsAfter,
    /// <summary>คำอธิบายวันครบกำหนดที่ใช้จริง เช่น "วันที่ 15 ของเดือนถัดไป", "150 วันหลังสิ้นรอบบัญชี"</summary>
    string DueDescription,
    /// <summary>กำลังใช้กติกานับเป็นจำนวนวัน — ช่อง "เดือน/วันที่" จะยังไม่มีผลจนกว่าจะตั้งค่าเอง</summary>
    bool UsesDaysAfterRule,
    bool RequireEvidence,        // ต้องแนบหลักฐานก่อนปิดงาน (effective)
    bool DefaultRequireEvidence, // ค่าเริ่มต้นของประเภทนี้
    string Source         // "default" = ค่าเริ่มต้นระบบ, "global" = ตั้งระดับทุกบริษัท, "company" = ตั้งเฉพาะบริษัท (override)
);

public record ComplianceDashboardDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int Year,
    IReadOnlyList<MonthSummaryDto> Months,
    int TotalOverdue,
    IReadOnlyList<ComplianceTaskDto> UpcomingDueSoon
);
