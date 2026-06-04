using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Leasing.Commands;

internal static class LeaseAccountGuard
{
    /// <summary>
    /// โหลด+ตรวจบัญชีที่ผูกกับสัญญา (หนี้สิน / ดอกเบี้ยรอตัด / ภาษีซื้อ / ดอกเบี้ยจ่าย):
    /// ต้องอยู่บริษัทเดียวกัน, active และ postable. คืน dict (AccountId → Account) เพื่อ map DTO.
    /// </summary>
    public static async Task<Dictionary<int, Account>> LoadAndValidateAsync(
        IApplicationDbContext db, int clientCompanyId, LeaseContractInput d, CancellationToken ct)
    {
        var ids = new List<int> { d.LiabilityAccountId, d.InterestExpenseAccountId };
        if (d.DeferredInterestAccountId.HasValue) ids.Add(d.DeferredInterestAccountId.Value);
        if (d.InputVatUndueAccountId.HasValue) ids.Add(d.InputVatUndueAccountId.Value);
        ids = ids.Distinct().ToList();

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
