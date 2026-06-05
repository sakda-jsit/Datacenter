using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Commands;

internal static class FixedAssetAccountGuard
{
    /// <summary>
    /// โหลด+ตรวจบัญชีที่ผูกกับสินทรัพย์ (ค่าเสื่อมสะสม / ค่าเสื่อมราคา / สินทรัพย์[ถ้ามี]):
    /// ต้องอยู่บริษัทเดียวกัน, active และ postable. คืน dict (AccountId → Account) เพื่อ map DTO.
    /// </summary>
    public static async Task<Dictionary<int, Account>> LoadAndValidateAsync(
        IApplicationDbContext db, int clientCompanyId, FixedAssetInput d, CancellationToken ct)
    {
        var ids = new List<int> { d.AccumDepreciationAccountId, d.DepreciationExpenseAccountId };
        if (d.AssetAccountId is { } assetAcc && assetAcc > 0) ids.Add(assetAcc);
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

    /// <summary>ถ้าระบุประเภทสินทรัพย์ ต้องมีอยู่จริงและ active</summary>
    public static async Task ValidateAssetTypeAsync(IApplicationDbContext db, int? assetTypeId, CancellationToken ct)
    {
        if (assetTypeId is not { } id) return;
        var exists = await db.AssetTypeMasters.AnyAsync(t => t.Id == id && t.IsActive, ct);
        if (!exists) throw new DomainException($"ไม่พบประเภทสินทรัพย์ Id={id}");
    }
}
