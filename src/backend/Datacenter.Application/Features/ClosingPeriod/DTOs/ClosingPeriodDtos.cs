using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.ClosingPeriod.DTOs;

/// <summary>
/// สถานะการปิดงวดของหนึ่งเดือน (คืนครบ 12 เดือนเสมอ เดือนที่ยังไม่มี record = Open)
/// </summary>
public record ClosingPeriodMonthDto(
    int Year,
    int Month,
    PeriodStatus Status,
    string StatusName,
    DateTime? ClosedAt,
    int? ClosedByUserId,
    string? ClosedByName,
    DateTime? BeginDate,
    DateTime? EndDate,
    bool SourceLocked);

public record ClosingPeriodOverviewDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int Year,
    bool IsDefined,
    IReadOnlyList<ClosingPeriodMonthDto> Months);

/// <summary>
/// ผลตรวจสอบความพร้อมก่อนปิดงวดหนึ่งรายการ
/// Severity: "Error" = บล็อกการปิด, "Warning" = เตือนแต่ปิดได้, "Info" = ยังไม่พร้อมตรวจ/ข้าม
/// </summary>
public record ClosingValidationItemDto(
    string Code,
    string Label,
    string Severity,
    bool Passed,
    string? Detail);

public record ClosingValidationDto(
    int ClientCompanyId,
    int Year,
    int Month,
    PeriodStatus CurrentStatus,
    bool CanClose,
    IReadOnlyList<ClosingValidationItemDto> Items);
