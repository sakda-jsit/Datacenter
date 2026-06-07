using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.InterestIncome.Commands;

internal static class InterestLoanAccountGuard
{
    /// <summary>โหลด+ตรวจบัญชี GL (อยู่บริษัทเดียวกัน, active, postable). คืน dictionary id→Account</summary>
    public static async Task<Dictionary<int, Account>> LoadAndValidateAsync(
        IApplicationDbContext db, int clientCompanyId, IEnumerable<int> accountIds, CancellationToken ct)
    {
        var ids = accountIds.Where(id => id > 0).Distinct().ToList();
        var accounts = await db.Accounts.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);

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
