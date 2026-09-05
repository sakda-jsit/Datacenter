using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Auth;

/// <summary>
/// ขอบเขตบริษัทที่ส่งกลับไปกับผลการเข้าสู่ระบบ.
/// สิทธิ์จริงบังคับที่ backend (<see cref="Common.Security.ICompanyAccessGuard"/>) เสมอ —
/// รายการนี้มีไว้ให้ frontend ซ่อน/ปิดปุ่มที่กดไปก็ 403 เท่านั้น.
/// </summary>
internal static class AuthCompanyScope
{
    /// <summary>
    /// บริษัทที่ผู้ใช้ "ดูแล" = ทำรายการและดูข้อมูลเงินเดือนได้.
    /// คืน null เมื่อเป็น Admin (ดูแลได้ทุกบริษัท)
    /// </summary>
    public static async Task<IReadOnlyList<int>?> OwnedCompanyIdsAsync(
        IApplicationDbContext db, User user, CancellationToken ct)
    {
        if (user.Role == UserRole.Admin)
            return null;

        return await db.CompanyUserAccesses.AsNoTracking()
            .Where(a => a.UserId == user.Id)
            .Select(a => a.ClientCompanyId)
            .ToListAsync(ct);
    }
}
