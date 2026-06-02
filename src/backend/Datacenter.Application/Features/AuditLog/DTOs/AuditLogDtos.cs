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
