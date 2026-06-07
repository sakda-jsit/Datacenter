using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.AuditLog.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.AuditLog.Queries;

/// <summary>
/// ตัวเลือกตัวกรอง audit log: ประเภทการกระทำ (Action) + โมดูล/รายการ (EntityName)
/// ที่มีอยู่จริงในขอบเขตที่ผู้ใช้เข้าถึง (กรองตามสิทธิ์บริษัทเดียวกับ list).
/// </summary>
public record GetAuditLogFilterOptionsQuery(int? ClientCompanyId) : IRequest<AuditLogFilterOptionsDto>;

public class GetAuditLogFilterOptionsQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetAuditLogFilterOptionsQuery, AuditLogFilterOptionsDto>
{
    public async Task<AuditLogFilterOptionsDto> Handle(GetAuditLogFilterOptionsQuery request, CancellationToken ct)
    {
        var query = await AuditLogQuerySupport.BuildFilteredAsync(
            db, accessGuard, request.ClientCompanyId, null, null, null, null, null, ct);

        var actions = await query.Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(ct);
        var entityNames = await query.Select(x => x.EntityName).Distinct().OrderBy(x => x).ToListAsync(ct);

        return new AuditLogFilterOptionsDto(actions, entityNames);
    }
}
