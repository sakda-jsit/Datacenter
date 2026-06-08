using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ComplianceCalendar.DTOs;

public record ComplianceTaskDto(
    int Id,
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    ComplianceTaskType TaskType,
    string TaskTypeName,
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
    bool IsOverdue
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
    bool Enabled,
    int? DueDay,          // วันครบกำหนด (override); null = ใช้ค่าเริ่มต้น
    int DefaultDueDay,    // ค่าเริ่มต้นของประเภทนี้ (0 = สิ้นเดือนถัดไป)
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
