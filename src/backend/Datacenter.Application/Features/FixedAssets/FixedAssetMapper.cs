using Datacenter.Application.Features.FixedAssets.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.FixedAssets;

internal static class FixedAssetMapper
{
    /// <summary>คัดลอกค่าจาก input ลง entity (ใช้ทั้ง create/update)</summary>
    public static void Apply(FixedAsset e, FixedAssetInput d)
    {
        e.AssetCode = d.AssetCode.Trim();
        e.AssetName = d.AssetName.Trim();
        e.AssetTypeId = d.AssetTypeId;
        e.AcquireDate = d.AcquireDate;
        e.Cost = d.Cost;
        e.SalvageValue = d.SalvageValue;
        e.BookRatePct = d.BookRatePct;
        e.TaxRatePct = d.TaxRatePct;
        e.AccumulatedBroughtForward = d.AccumulatedBroughtForward;
        e.BroughtForwardYear = d.BroughtForwardYear;
        e.AssetGroupCode = string.IsNullOrWhiteSpace(d.AssetGroupCode) ? null : d.AssetGroupCode.Trim();
        e.CategoryCode = string.IsNullOrWhiteSpace(d.CategoryCode) ? null : d.CategoryCode.Trim();
        e.Status = d.Status;
        e.DisposalDate = d.DisposalDate;
        e.DisposalProceeds = d.DisposalProceeds;
        e.DisposalNote = string.IsNullOrWhiteSpace(d.DisposalNote) ? null : d.DisposalNote.Trim();
        e.AssetAccountId = d.AssetAccountId;
        e.AccumDepreciationAccountId = d.AccumDepreciationAccountId;
        e.DepreciationExpenseAccountId = d.DepreciationExpenseAccountId;
        e.Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim();
        e.AttachmentPath = string.IsNullOrWhiteSpace(d.AttachmentPath) ? null : d.AttachmentPath.Trim();
        e.IsActive = d.IsActive;
    }

    public static FixedAssetListItemDto ToListItem(FixedAsset e, string? typeName)
        => new(e.Id, e.AssetCode, e.AssetName, typeName, e.CategoryCode, e.AcquireDate, e.Cost, e.BookRatePct, e.Status, e.IsActive);

    public static FixedAssetDto ToDto(FixedAsset e, string? typeName, IReadOnlyDictionary<int, Account> accounts)
    {
        string? Code(int? id) => id.HasValue && accounts.TryGetValue(id.Value, out var a) ? a.AccountCode : null;

        return new FixedAssetDto(
            e.Id, e.ClientCompanyId, e.AssetCode, e.AssetName, e.AssetTypeId, typeName,
            e.AcquireDate, e.Cost, e.SalvageValue, e.BookRatePct, e.TaxRatePct,
            e.AccumulatedBroughtForward, e.BroughtForwardYear, e.AssetGroupCode, e.CategoryCode, e.Status,
            e.DisposalDate, e.DisposalProceeds, e.DisposalNote,
            e.AssetAccountId, Code(e.AssetAccountId),
            e.AccumDepreciationAccountId, Code(e.AccumDepreciationAccountId),
            e.DepreciationExpenseAccountId, Code(e.DepreciationExpenseAccountId),
            e.Notes, e.AttachmentPath, e.IsActive);
    }

    public static AssetTypeDto ToDto(AssetTypeMaster t)
        => new(t.Id, t.Code, t.Name, t.DefaultBookRatePct, t.DefaultTaxRatePct, t.DefaultUsefulLifeYears, t.IsActive);
}
