using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Models;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.AuditLog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditLog.Queries;

public class GetAuditLogsQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (request.ClientCompanyId.HasValue)
        {
            // ระบุบริษัทเจาะจง → ตรวจสิทธิ์ก่อนกรอง
            await accessGuard.EnsureAccessAsync(request.ClientCompanyId.Value, ct);
            query = query.Where(x => x.ClientCompanyId == request.ClientCompanyId.Value);
        }
        else
        {
            // ไม่ระบุบริษัท → จำกัดเฉพาะบริษัทที่ผู้ใช้เข้าถึงได้ (null = Admin เห็นทั้งหมด)
            var accessibleIds = await accessGuard.GetAccessibleCompanyIdsAsync(ct);
            if (accessibleIds is not null)
                query = query.Where(x => x.ClientCompanyId != null && accessibleIds.Contains(x.ClientCompanyId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(x => x.Action == request.Action);

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(x => x.EntityName == request.EntityName);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Username.Contains(term)
                                  || (x.EntityId != null && x.EntityId.Contains(term)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
        {
            // ToDate เป็นวันสิ้นสุดแบบ inclusive → ครอบคลุมทั้งวัน
            var toExclusive = request.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toExclusive);
        }

        var total = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        // ดึงชื่อบริษัทในครั้งเดียว เลี่ยง N+1 (AuditLog ไม่มี navigation ไป ClientCompany)
        var companyIds = logs.Where(l => l.ClientCompanyId.HasValue)
            .Select(l => l.ClientCompanyId!.Value).Distinct().ToList();
        var companyNames = await db.ClientCompanies
            .AsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.LegalName, ct);

        var items = logs.Select(l => new AuditLogDto(
            l.Id,
            l.ClientCompanyId,
            l.ClientCompanyId.HasValue && companyNames.TryGetValue(l.ClientCompanyId.Value, out var name) ? name : null,
            l.UserId,
            l.Username,
            l.Action,
            l.EntityName,
            l.EntityId,
            l.BeforeValue,
            l.AfterValue,
            l.CreatedAt)).ToList();

        return PaginatedResult<AuditLogDto>.Create(items, total, request.PageNumber, request.PageSize);
    }
}
