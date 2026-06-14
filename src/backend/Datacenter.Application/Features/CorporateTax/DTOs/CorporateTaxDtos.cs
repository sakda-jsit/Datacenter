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

/// <summary>ผู้ลงนามรับผิดชอบงบ (ผู้สอบบัญชี + ผู้ทำบัญชี) ของ (บริษัท, ปีงบ) — ต่อรอบปี.</summary>
public record CompanyAuditorDto(
    int ClientCompanyId,
    int FiscalYear,
    string AuditorName,
    string? AuditorLicenseNo,
    string? AuditorTaxId,
    string? BookkeeperName,
    string? BookkeeperTaxId,
    DateTime? SignDate,
    string? Note,
    /// <summary>มีบันทึกของปีนี้แล้วหรือไม่ (false = ค่าว่างเริ่มต้น)</summary>
    bool Exists);

/// <summary>ข้อมูล input บันทึกผู้สอบบัญชี + ผู้ทำบัญชีต่อรอบปี.</summary>
public record CompanyAuditorInput(
    string AuditorName,
    string? AuditorLicenseNo,
    string? AuditorTaxId,
    string? BookkeeperName,
    string? BookkeeperTaxId,
    DateTime? SignDate,
    string? Note);

/// <summary>ข้อมูลสำหรับเติมแบบ ภ.ง.ด.50 (PDF) เฟส A — หน้า 1 (หัว + ที่อยู่) + หน้า 2 (การคำนวณภาษี).</summary>
public record Pnd50FormData(
    string CompanyName,
    string TaxId,
    bool IsHeadOffice,
    string? BusinessActivity,
    string? IsicCode,
    string? AuditorName,
    string? AuditorLicenseNo,
    string? AuditorTaxId,
    string? BookkeeperName,
    string? BookkeeperTaxId,
    // ที่อยู่ (แยกช่อง — แยกจาก Address flat ด้วย ThaiAddressParser)
    string? HouseNo,
    string? Moo,
    string? Soi,
    string? Road,
    string? SubDistrict,
    string? District,
    string? Province,
    string? PostalCode,
    string? Phone,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal NetTaxableIncome,   // ฐานภาษี (กำไรสุทธิที่ต้องเสียภาษี)
    decimal TaxAmount,          // ภาษีที่คำนวณได้
    decimal WhtCredit,          // ภาษีหัก ณ ที่จ่าย
    decimal TotalCredit,        // รวมรายการหัก
    decimal NetPayable);        // คงเหลือชำระเพิ่ม (>0) / ชำระเกิน (&lt;0)
