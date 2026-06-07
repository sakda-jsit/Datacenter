using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.AuditLog.DTOs;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditLog.Queries;

/// <summary>
/// ตัวช่วยร่วมสำหรับ audit log: สร้าง query ที่ผ่านการกรอง (รวมจำกัดสิทธิ์บริษัท)
/// และแปลง entity → DTO พร้อมชื่อบริษัท — ใช้ทั้ง list, export, filter-options.
/// </summary>
internal static class AuditLogQuerySupport
{
    /// <summary>สร้าง IQueryable ที่กรองตามสิทธิ์บริษัท + ตัวกรองทั้งหมด (ยังไม่เรียงลำดับ/แบ่งหน้า)</summary>
    public static async Task<IQueryable<Domain.Entities.AuditLog>> BuildFilteredAsync(
        IApplicationDbContext db, ICompanyAccessGuard accessGuard,
        int? clientCompanyId, string? action, string? entityName, string? search,
        DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (clientCompanyId.HasValue)
        {
            await accessGuard.EnsureAccessAsync(clientCompanyId.Value, ct);
            query = query.Where(x => x.ClientCompanyId == clientCompanyId.Value);
        }
        else
        {
            var accessibleIds = await accessGuard.GetAccessibleCompanyIdsAsync(ct);
            if (accessibleIds is not null)
                query = query.Where(x => x.ClientCompanyId != null && accessibleIds.Contains(x.ClientCompanyId.Value));
        }

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(x => x.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Username.Contains(term)
                                  || (x.EntityId != null && x.EntityId.Contains(term)));
        }

        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toExclusive);
        }

        return query;
    }

    /// <summary>แปลง audit log entities → DTO พร้อมเติมชื่อบริษัท (ดึงครั้งเดียว เลี่ยง N+1)</summary>
    public static async Task<List<AuditLogDto>> MapAsync(
        IApplicationDbContext db, List<Domain.Entities.AuditLog> logs, CancellationToken ct)
    {
        var companyIds = logs.Where(l => l.ClientCompanyId.HasValue)
            .Select(l => l.ClientCompanyId!.Value).Distinct().ToList();
        var companyNames = await db.ClientCompanies.AsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.LegalName, ct);

        return logs.Select(l => new AuditLogDto(
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
    }
}
