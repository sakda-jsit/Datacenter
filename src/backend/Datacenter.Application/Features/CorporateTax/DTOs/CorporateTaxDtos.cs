using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.CorporateTax.DTOs;

/// <summary>หนึ่งขั้นของอัตราภาษี (สำหรับแสดงการคำนวณแบบขั้นบันได).</summary>
public record TaxBracketDto(string Label, decimal Base, decimal RatePct, decimal Tax);

/// <summary>ผลการคำนวณภาษีเงินได้นิติบุคคล (จาก CorporateTaxEngine).</summary>
public record TaxComputationResult(
    decimal NetProfitBeforeTax,
    decimal AddBackTotal,
    decimal DeductionTotal,
    /// <summary>กำไรสุทธิทางภาษีก่อนหักขาดทุนสะสม</summary>
    decimal AdjustedProfit,
    decimal LossBroughtForward,
    decimal LossUsed,
    /// <summary>เงินได้สุทธิเพื่อเสียภาษี</summary>
    decimal NetTaxableIncome,
    IReadOnlyList<TaxBracketDto> Brackets,
    decimal TaxAmount,
    decimal WhtCredit,
    /// <summary>ภาษีค้างชำระ (>0) / ชำระเกินขอคืน (&lt;0)</summary>
    decimal NetPayable,
    /// <summary>ผลขาดทุนสะสมยกไปปีถัดไป</summary>
    decimal LossCarriedForward);

/// <summary>รายการปรับปรุงทางภาษีหนึ่งบรรทัด (input/output).</summary>
public record TaxAdjustmentLineDto(
    int Id,
    TaxAdjustmentKind Kind,
    string Description,
    decimal Amount,
    int SortOrder);

/// <summary>กระดาษทำการคำนวณภาษี + ผลลัพธ์ (สำหรับแสดงผล).</summary>
public record TaxComputationDto(
    int ClientCompanyId,
    string ClientName,
    int FiscalYear,
    TaxRateScheme RateScheme,
    decimal? CustomRatePct,
    decimal LossBroughtForward,
    decimal WhtCredit,
    string? Note,
    IReadOnlyList<TaxAdjustmentLineDto> Lines,
    TaxComputationResult Result,
    /// <summary>มีข้อมูลงบกำไรขาดทุนให้คำนวณหรือไม่ (false = ยังไม่นำเข้า/ยังไม่ post)</summary>
    bool HasProfitLoss,
    /// <summary>คำเตือนข้อมูลไม่ครบก่อนสรุปภาษี</summary>
    IReadOnlyList<string> Warnings);

/// <summary>ข้อมูล input สำหรับบันทึกกระดาษทำการภาษี.</summary>
public record TaxComputationInput(
    TaxRateScheme RateScheme,
    decimal? CustomRatePct,
    decimal LossBroughtForward,
    decimal WhtCredit,
    string? Note,
    IReadOnlyList<TaxAdjustmentLineInput> Lines);

public record TaxAdjustmentLineInput(
    TaxAdjustmentKind Kind,
    string Description,
    decimal Amount,
    int SortOrder);
