using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Adjustments.Commands;

internal static class AdjustmentAccountGuard
{
    /// <summary>
    /// โหลดบัญชีที่อ้างถึงทั้งหมด ตรวจว่าอยู่ในบริษัทเดียวกัน, active และ postable.
    /// คืน dict (AccountId → Account) เพื่อใช้ map กลับเป็น DTO.
    /// </summary>
    public static async Task<Dictionary<int, Account>> LoadAndValidateAsync(
        IApplicationDbContext db,
        int clientCompanyId,
        IReadOnlyList<AdjustmentLineInput> lines,
        CancellationToken ct)
    {
        var ids = lines.Select(l => l.AccountId).Distinct().ToList();

        var accounts = await db.Accounts
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        foreach (var id in ids)
        {
            if (!accounts.TryGetValue(id, out var acc))
                throw new DomainException($"ไม่พบบัญชี Id={id}");
            if (acc.ClientCompanyId != clientCompanyId)
                throw new DomainException($"บัญชี {acc.AccountCode} ไม่ได้อยู่ในบริษัทนี้");
            if (!acc.IsActive)
                throw new DomainException($"บัญชี {acc.AccountCode} ถูกปิดใช้งาน");
            if (!acc.IsPostable)
                throw new DomainException($"บัญชี {acc.AccountCode} เป็นบัญชีหัวข้อ (ลงรายการไม่ได้)");
        }

        return accounts;
    }
}
