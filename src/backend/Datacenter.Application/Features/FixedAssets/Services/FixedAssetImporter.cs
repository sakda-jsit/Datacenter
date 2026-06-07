using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Application.Features.Import.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FixedAssets.Services;

/// <summary>
/// นำเข้าทะเบียนสินทรัพย์ถาวรจาก Express FAMAS.DBF (upsert ตามรหัสสินทรัพย์) — ใช้ร่วมใน
/// pipeline นำเข้าข้อมูลกลาง (StartExpressImport). ไม่ผ่าน staging เพราะ FA register เป็น
/// source of truth ตรง ๆ. ไม่เรียก SaveChanges — ผู้เรียกบันทึกรวมกับ batch.
/// </summary>
public static class FixedAssetImporter
{
    public static async Task<FixedAssetImportResultDto> ImportAsync(
        IApplicationDbContext db,
        IExpressDbfAdapter dbfAdapter,
        string folderPath,
        int clientCompanyId,
        int fiscalYear,
        string username,
        CancellationToken ct)
    {
        var rows = await dbfAdapter.ReadFixedAssetsAsync(folderPath, ct);
        if (rows.Count == 0)
            return new FixedAssetImportResultDto(0, 0, 0, [], "ไม่มีข้อมูลสินทรัพย์ใน FAMAS.DBF");

        var mappings = await db.AssetAccountMappings
            .Where(m => m.ClientCompanyId == clientCompanyId)
            .ToDictionaryAsync(m => m.CategoryCode.ToUpper(), ct);

        var existing = await db.FixedAssets
            .Where(a => a.ClientCompanyId == clientCompanyId)
            .ToDictionaryAsync(a => a.AssetCode, ct);

        var created = 0;
        var updated = 0;
        var unmapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var (status, disposalDate, proceeds) = ResolveDisposal(row);
            var category = string.IsNullOrWhiteSpace(row.CategoryCode) ? null : row.CategoryCode.Trim();

            mappings.TryGetValue((category ?? "").ToUpper(), out var map);
            if (category is not null && map is null) unmapped.Add(category);

            var acquire = row.AcquireDate ?? new DateTime(fiscalYear, 1, 1);

            bool isNew;
            if (!existing.TryGetValue(row.AssetCode, out var asset))
            {
                asset = new FixedAsset { ClientCompanyId = clientCompanyId, CreatedBy = username };
                db.FixedAssets.Add(asset);
                created++;
                isNew = true;
            }
            else
            {
                asset.ModifiedBy = username;
                asset.ModifiedAt = DateTime.UtcNow;
                updated++;
                isNew = false;
            }

            // ฟิลด์ที่ Express เป็นเจ้าของ — refresh ทุกครั้ง (Express = source of truth)
            asset.IsFromExpress = true;
            asset.AssetCode = row.AssetCode;
            asset.AssetName = row.AssetName;
            asset.AssetGroupCode = string.IsNullOrWhiteSpace(row.GroupCode) ? null : row.GroupCode.Trim();
            asset.CategoryCode = category;
            asset.AcquireDate = acquire;
            asset.Cost = row.Cost;
            asset.SalvageValue = row.Salvage;
            asset.AccumulatedBroughtForward = row.AccumulatedBroughtForward;
            asset.BroughtForwardYear = fiscalYear;
            asset.Status = status;
            asset.DisposalDate = disposalDate;
            asset.DisposalProceeds = proceeds;
            asset.IsActive = true;

            // อัตราค่าเสื่อม: Express เก็บอัตราเดียว → seed ทั้งชุดบัญชี/ภาษี "เฉพาะตอนสร้างใหม่"
            // ตอน re-import ไม่ทับ เพื่อคง override (book/tax split) ที่ผู้ใช้ปรับไว้
            if (isNew)
            {
                asset.BookRatePct = row.RatePct;
                asset.TaxRatePct = row.RatePct;
            }

            // เติมบัญชี GL จากการแมพหมวด (ถ้ายังไม่แมพ = ว่าง → ตั้งภายหลังที่หน้า "แมพบัญชี")
            // ไม่ทับบัญชีที่ตั้งไว้แล้ว (กรณี re-import)
            if (asset.AccumDepreciationAccountId == 0 && map?.AccumDepreciationAccountId is { } accum)
                asset.AccumDepreciationAccountId = accum;
            if (asset.DepreciationExpenseAccountId == 0 && map?.DepreciationExpenseAccountId is { } exp)
                asset.DepreciationExpenseAccountId = exp;
            if (asset.AssetAccountId is null or 0 && map?.AssetAccountId is { } assetAcc)
                asset.AssetAccountId = assetAcc;
        }

        var msg = $"สินทรัพย์ {rows.Count} รายการ (ใหม่ {created}, อัปเดต {updated})"
                + (unmapped.Count > 0 ? $", หมวดที่ยังไม่แมพบัญชี: {string.Join(", ", unmapped)}" : "");

        return new FixedAssetImportResultDto(rows.Count, created, updated, unmapped.OrderBy(x => x).ToList(), msg);
    }

    /// <summary>SALDAT+SALAMT&gt;0 → ขาย; SALDAT เฉย ๆ → จำหน่าย; อื่น → ใช้งาน</summary>
    private static (FixedAssetStatus, DateTime?, decimal?) ResolveDisposal(ExpressFixedAssetDto row)
    {
        if (row.SaleDate is not { } d) return (FixedAssetStatus.Active, null, null);
        return row.SaleAmount > 0
            ? (FixedAssetStatus.Sold, d, row.SaleAmount)
            : (FixedAssetStatus.Disposed, d, row.SaleAmount);
    }
}
