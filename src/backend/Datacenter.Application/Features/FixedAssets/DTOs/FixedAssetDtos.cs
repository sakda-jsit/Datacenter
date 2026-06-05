using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.FixedAssets.DTOs;

// ── Asset type master ─────────────────────────────────────────────────────────

public record AssetTypeDto(
    int Id,
    string Code,
    string Name,
    decimal DefaultBookRatePct,
    decimal DefaultTaxRatePct,
    int DefaultUsefulLifeYears,
    bool IsActive);

// ── Asset (list + full) ───────────────────────────────────────────────────────

public record FixedAssetListItemDto(
    int Id,
    string AssetCode,
    string AssetName,
    string? AssetTypeName,
    string? CategoryCode,
    DateTime AcquireDate,
    decimal Cost,
    decimal BookRatePct,
    FixedAssetStatus Status,
    bool IsActive);

/// <summary>ทะเบียนสินทรัพย์ + ความสดของข้อมูล (snapshot เวลานำเข้า FAMAS ล่าสุด)</summary>
public record FixedAssetListDto(
    IReadOnlyList<FixedAssetListItemDto> Items,
    DateTime? DataAsOf);

public record FixedAssetDto(
    int Id,
    int ClientCompanyId,
    string AssetCode,
    string AssetName,
    int? AssetTypeId,
    string? AssetTypeName,
    DateTime AcquireDate,
    decimal Cost,
    decimal SalvageValue,
    decimal BookRatePct,
    decimal TaxRatePct,
    decimal AccumulatedBroughtForward,
    int BroughtForwardYear,
    string? AssetGroupCode,
    string? CategoryCode,
    FixedAssetStatus Status,
    DateTime? DisposalDate,
    decimal? DisposalProceeds,
    string? DisposalNote,
    int? AssetAccountId,
    string? AssetAccountCode,
    int AccumDepreciationAccountId,
    string? AccumDepreciationAccountCode,
    int DepreciationExpenseAccountId,
    string? DepreciationExpenseAccountCode,
    string? Notes,
    string? AttachmentPath,
    bool IsActive);

/// <summary>ฟิลด์ที่แก้ไขได้ของสินทรัพย์ (ใช้ทั้ง create/update)</summary>
public record FixedAssetInput(
    string AssetCode,
    string AssetName,
    int? AssetTypeId,
    DateTime AcquireDate,
    decimal Cost,
    decimal SalvageValue,
    decimal BookRatePct,
    decimal TaxRatePct,
    decimal AccumulatedBroughtForward,
    int BroughtForwardYear,
    string? AssetGroupCode,
    string? CategoryCode,
    FixedAssetStatus Status,
    DateTime? DisposalDate,
    decimal? DisposalProceeds,
    string? DisposalNote,
    int? AssetAccountId,
    int AccumDepreciationAccountId,
    int DepreciationExpenseAccountId,
    string? Notes,
    string? AttachmentPath,
    bool IsActive);

// ── Depreciation (per set) ─────────────────────────────────────────────────────

/// <summary>ยอดค่าเสื่อม ณ สิ้นปีงบที่ขอ (ชุดใดชุดหนึ่ง)</summary>
public record DepreciationAsOfDto(
    decimal OpeningAccumulated,   // ค่าเสื่อมสะสมต้นปี (ถึงสิ้นปีก่อน)
    decimal Charge,               // ค่าเสื่อมงวด (ปีนี้)
    decimal ClosingAccumulated,   // ค่าเสื่อมสะสมสิ้นปี
    decimal NetBookValue,         // มูลค่าสุทธิ = ราคาทุน − ค่าเสื่อมสะสมสิ้นปี
    bool FullyDepreciated);

/// <summary>หนึ่งปีในตารางค่าเสื่อม</summary>
public record DepreciationYearDto(
    int Year,
    decimal OpeningAccumulated,
    decimal Charge,
    decimal ClosingAccumulated,
    decimal NetBookValue);

/// <summary>ผลการจำหน่าย/ขาย (กำไร/ขาดทุนคำนวณอัตโนมัติจากชุดบัญชี)</summary>
public record DisposalResultDto(
    DateTime DisposalDate,
    FixedAssetStatus Status,
    decimal Proceeds,
    decimal NetBookValueAtDisposal,
    decimal GainLoss);          // ราคาขาย − มูลค่าสุทธิ ณ วันจำหน่าย (>0 กำไร, <0 ขาดทุน)

public record FixedAssetDetailDto(
    FixedAssetDto Asset,
    int FiscalYear,
    DepreciationAsOfDto Book,
    DepreciationAsOfDto Tax,
    IReadOnlyList<DepreciationYearDto> BookSchedule,
    IReadOnlyList<DepreciationYearDto> TaxSchedule,
    DisposalResultDto? Disposal);

// ── Express import ────────────────────────────────────────────────────────────

public record FixedAssetImportResultDto(
    int Read,
    int Created,
    int Updated,
    IReadOnlyList<string> UnmappedCategories,   // ACCCOD ที่ยังไม่ได้แมพ→บัญชี GL (สินทรัพย์เหล่านี้บัญชี = ว่าง)
    string Message);

// ── Asset account mapping (Express ACCCOD → GL) ───────────────────────────────

public record AssetAccountMappingDto(
    int Id,
    int ClientCompanyId,
    string CategoryCode,
    string? Description,
    int? AssetAccountId,
    string? AssetAccountCode,
    int? AccumDepreciationAccountId,
    string? AccumDepreciationAccountCode,
    int? DepreciationExpenseAccountId,
    string? DepreciationExpenseAccountCode,
    int AssetCount);          // จำนวนสินทรัพย์ที่ใช้หมวดนี้

public record AssetAccountMappingInput(
    string CategoryCode,
    string? Description,
    int? AssetAccountId,
    int? AccumDepreciationAccountId,
    int? DepreciationExpenseAccountId);

// ── Workpaper (RPT-013) + GL comparison ───────────────────────────────────────

public record FixedAssetWorkpaperRowDto(
    int AssetId,
    string AssetCode,
    string AssetName,
    string? AssetTypeName,
    DateTime AcquireDate,
    decimal Cost,
    FixedAssetStatus Status,
    DepreciationAsOfDto Book,
    DepreciationAsOfDto Tax,
    DisposalResultDto? Disposal);

/// <summary>สรุปตามประเภทสินทรัพย์ (เชื่อม NOTE2) — ใช้ชุดบัญชี</summary>
public record FixedAssetTypeSummaryDto(
    string AssetTypeName,
    int Count,
    decimal Cost,
    decimal BookClosingAccumulated,
    decimal BookNetBookValue,
    decimal ChargeInYear);

public record FixedAssetGlCompareDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    string Role,                 // AccumDepreciation / DepreciationExpense
    decimal ScheduleAmount,      // accum dep: ยอดสะสมสิ้นปี; dep expense: ค่าเสื่อมงวดปีนี้
    decimal GlAmount,            // accum dep: ยอดสะสมถึงสิ้นปี; dep expense: movement ในปี
    decimal Diff);

public record FixedAssetWorkpaperDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int FiscalYear,
    IReadOnlyList<FixedAssetWorkpaperRowDto> Rows,
    IReadOnlyList<FixedAssetTypeSummaryDto> TypeSummary,
    IReadOnlyList<FixedAssetGlCompareDto> GlComparison)
{
    public decimal TotalCost => Rows.Sum(r => r.Cost);
    public decimal TotalBookClosingAccumulated => Rows.Sum(r => r.Book.ClosingAccumulated);
    public decimal TotalBookNetBookValue => Rows.Sum(r => r.Book.NetBookValue);
    public decimal TotalBookCharge => Rows.Sum(r => r.Book.Charge);
    public decimal TotalTaxCharge => Rows.Sum(r => r.Tax.Charge);
    public bool HasDifference => GlComparison.Any(g => Math.Round(g.Diff, 2) != 0);
}
