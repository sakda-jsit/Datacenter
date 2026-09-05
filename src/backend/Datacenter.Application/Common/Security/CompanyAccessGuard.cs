using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Common.Security;

public class CompanyAccessGuard(IApplicationDbContext db, ICurrentUserService currentUser)
    : ICompanyAccessGuard
{
    /// <summary>ระดับ "ดู" — ผู้ใช้ที่เข้าระบบแล้วดูข้อมูลได้ทุกบริษัท</summary>
    public Task EnsureAccessAsync(int clientCompanyId, CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            throw new ForbiddenException("ไม่พบผู้ใช้ปัจจุบัน");
        return Task.CompletedTask;
    }

    /// <summary>ระดับ "ผู้ดูแล" — ต้องเป็น Admin หรือมีสิทธิ์ดูแลบริษัทนั้น</summary>
    public async Task EnsureOwnerAccessAsync(int clientCompanyId, CancellationToken ct = default)
    {
        if (currentUser.Role == UserRole.Admin)
            return;

        var userId = currentUser.UserId;
        bool isOwner = userId is not null && await db.CompanyUserAccesses
            .AnyAsync(a => a.UserId == userId && a.ClientCompanyId == clientCompanyId, ct);

        if (!isOwner)
            throw new ForbiddenException(
                "คุณไม่ได้เป็นผู้ดูแลบริษัทนี้ จึงทำรายการหรือดูข้อมูลส่วนนี้ไม่ได้ — ดูข้อมูลอย่างเดียวได้ตามปกติ");
    }

    /// <summary>ทุกคนดูได้ทุกบริษัท → null เสมอ (null = ไม่ต้องกรอง)</summary>
    public Task<IReadOnlyList<int>?> GetAccessibleCompanyIdsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<int>?>(null);

    public async Task<IReadOnlyList<int>?> GetOwnedCompanyIdsAsync(CancellationToken ct = default)
    {
        if (currentUser.Role == UserRole.Admin)
            return null;   // ดูแลได้ทุกบริษัท

        return await db.CompanyUserAccesses
            .Where(a => a.UserId == currentUser.UserId)
            .Select(a => a.ClientCompanyId)
            .ToListAsync(ct);
    }
}
