namespace Datacenter.Application.Features.AuditLog.DTOs;

public record AuditLogDto(
    long Id,
    int? ClientCompanyId,
    string? ClientName,
    int? UserId,
    string Username,
    string Action,
    string EntityName,
    string? EntityId,
    string? BeforeValue,
    string? AfterValue,
    DateTime CreatedAt);

/// <summary>ชุด audit log เต็มตามตัวกรอง (สำหรับ export ให้ผู้สอบบัญชี) + จำนวนรวมเพื่อยืนยันความครบ</summary>
public record AuditLogExportDto(
    IReadOnlyList<AuditLogDto> Items,
    int TotalCount,
    bool Capped,
    int Cap);

/// <summary>ตัวเลือกตัวกรอง (ประเภทรายการ/โมดูล) ที่มีจริงในขอบเขตที่ผู้ใช้เข้าถึง</summary>
public record AuditLogFilterOptionsDto(
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> EntityNames);
