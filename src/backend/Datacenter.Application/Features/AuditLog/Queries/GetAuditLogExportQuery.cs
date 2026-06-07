using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.AuditLog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditLog.Queries;

/// <summary>
/// ดึง audit log "ทั้งชุด" ตามตัวกรอง (ไม่แบ่งหน้า) สำหรับ export ให้ผู้สอบบัญชี (docs/18 A-034).
/// จำกัดไม่เกิน Cap รายการเพื่อกันโหลดหนัก — ถ้าเกินจะตั้ง Capped=true ให้ frontend เตือน.
/// </summary>
public record GetAuditLogExportQuery(
    int? ClientCompanyId,
    string? Action,
    string? EntityName,
    string? Search,
    DateTime? FromDate,
    DateTime? ToDate) : IRequest<AuditLogExportDto>;

public class GetAuditLogExportQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetAuditLogExportQuery, AuditLogExportDto>
{
    private const int Cap = 50_000;

    public async Task<AuditLogExportDto> Handle(GetAuditLogExportQuery request, CancellationToken ct)
    {
        var query = await AuditLogQuerySupport.BuildFilteredAsync(
            db, accessGuard,
            request.ClientCompanyId, request.Action, request.EntityName, request.Search,
            request.FromDate, request.ToDate, ct);

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(Cap)
            .ToListAsync(ct);

        var items = await AuditLogQuerySupport.MapAsync(db, logs, ct);
        return new AuditLogExportDto(items, total, Capped: total > Cap, Cap);
    }
}
