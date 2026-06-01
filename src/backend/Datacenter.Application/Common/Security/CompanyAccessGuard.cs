using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Common.Security;

public class CompanyAccessGuard(IApplicationDbContext db, ICurrentUserService currentUser)
    : ICompanyAccessGuard
{
    public async Task EnsureAccessAsync(int clientCompanyId, CancellationToken ct = default)
    {
        // Admin เข้าถึงได้ทุกบริษัท
        if (currentUser.Role == UserRole.Admin)
            return;

        var userId = currentUser.UserId;
        var hasAccess = userId is not null && await db.CompanyUserAccesses
            .AnyAsync(a => a.UserId == userId && a.ClientCompanyId == clientCompanyId, ct);

        if (!hasAccess)
            throw new ForbiddenException(
                $"ไม่มีสิทธิ์เข้าถึงข้อมูลของบริษัทรหัส {clientCompanyId}");
    }

    public async Task<IReadOnlyList<int>?> GetAccessibleCompanyIdsAsync(CancellationToken ct = default)
    {
        // Admin → null = เข้าถึงได้ทุกบริษัท (ไม่ต้องกรอง)
        if (currentUser.Role == UserRole.Admin)
            return null;

        return await db.CompanyUserAccesses
            .Where(a => a.UserId == currentUser.UserId)
            .Select(a => a.ClientCompanyId)
            .ToListAsync(ct);
    }
}
