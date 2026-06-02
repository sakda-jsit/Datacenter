using Datacenter.Application.Common.Models;
using Datacenter.Application.Features.AuditLog.DTOs;
using MediatR;

namespace Datacenter.Application.Features.AuditLog.Queries;

/// <summary>
/// ดึง audit log แบบแบ่งหน้า — ไม่ implement IRequireCompanyAccess เพราะเป็น list ข้ามบริษัท
/// การจำกัดสิทธิ์ทำภายใน handler ผ่าน ICompanyAccessGuard (เหมือน GetImportHistory)
/// </summary>
public record GetAuditLogsQuery(
    int? ClientCompanyId,
    string? Action,
    string? EntityName,
    string? Search,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedResult<AuditLogDto>>;
