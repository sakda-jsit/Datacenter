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

/// <summary>
/// ผู้ลงนามของ (บริษัท, ปีงบ) — master + ค่าเริ่มต้นบริษัท + override รายปี.
/// Resolved* = ค่าที่ใช้จริง (override ปีนี้ ?? ค่าเริ่มต้นบริษัท).
/// </summary>
public record CompanySignersDto(
    int ClientCompanyId,
    int FiscalYear,
    int? DefaultAuditorId,
    int? DefaultBookkeeperId,
    int? YearAuditorId,
    int? YearBookkeeperId,
    int? ResolvedAuditorId,
    int? ResolvedBookkeeperId,
    DateTime? SignDate,
    /// <summary>ปีนี้มี override ผู้ลงนามต่างจากค่าเริ่มต้นหรือไม่</summary>
    bool HasYearOverride);

/// <summary>ตั้งผู้ลงนามประจำบริษัท (ค่าเริ่มต้นทุกปี).</summary>
public record CompanyDefaultSignersInput(int? AuditorId, int? BookkeeperId);

/// <summary>หนึ่งแถวในภาพรวมมอบหมายผู้ลงนาม (ผู้สอบ/ผู้ทำบัญชี ประจำของแต่ละบริษัท).</summary>
public record SignerAssignmentDto(
    int CompanyId,
    string CompanyName,
    string CompanyCode,
    int? DefaultAuditorId,
    string? DefaultAuditorName,
    int? DefaultBookkeeperId,
    string? DefaultBookkeeperName,
    /// <summary>จำนวนปีที่มี override ต่างจากค่าเริ่มต้น</summary>
    int OverrideYears);

/// <summary>บันทึกผู้ลงนามเฉพาะรอบปี (override + วันที่ในรายงาน). AuditorId/BookkeeperId null = ใช้ค่าเริ่มต้น.</summary>
public record CompanyYearSignersInput(int? AuditorId, int? BookkeeperId, DateTime? SignDate);

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
    string? AuditFirmTaxId,        // เลขผู้เสียภาษีสำนักงานสอบบัญชี (field 49)
    string? BookkeepingFirmTaxId,  // เลขผู้เสียภาษีสำนักงานทำบัญชี = โปรไฟล์สำนักงาน (field 52)
    DateTime? AuditorSignDate,   // วันที่ในรายงานของผู้ตรวจสอบและรับรองบัญชี (field 46-48)
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
    decimal NetPayable,         // คงเหลือชำระเพิ่ม (>0) / ชำระเกิน (&lt;0)
    // สำหรับติ๊ก checkbox หน้า 2
    TaxRateScheme RateScheme,   // อัตราภาษี (SME → ติ๊กกรณีลดอัตรา 1.2)
    bool IsNetProfit,           // กำไรสุทธิ (true) / ขาดทุนสุทธิ (false)
    // หน้า 3: รายการที่ 3 reconciliation (null = ไม่มีงบ/ไม่เติม)
    Pnd50Page3Data? Page3 = null);

/// <summary>ข้อมูลหน้า 3 (รายการที่ 3) — reconciliation กำไรบัญชี → เงินได้สุทธิเพื่อเสียภาษี.</summary>
public record Pnd50Page3Data(
    decimal Revenue,             // 1. รายได้โดยตรง
    decimal Cogs,                // 2. ต้นทุนขาย
    decimal GrossProfit,         // 3. กำไร(ขาดทุน)ขั้นต้น
    decimal Sga,                 // 8. รายจ่ายขายและบริหาร (รวมต้นทุนการเงิน)
    decimal NetAccountingProfit, // 9. กำไร(ขาดทุน)สุทธิตามบัญชี (= ก่อนภาษี)
    decimal AddBack,             // 11. บวก รายจ่ายต้องห้าม
    decimal Deduction,           // 13. หัก รายได้ยกเว้น/หักเพิ่ม
    decimal AdjustedProfit,      // 14. รวม
    decimal LossUsed,            // 15. หัก ขาดทุนยกมา
    decimal NetTaxableIncome);   // 16./21. เงินได้สุทธิเพื่อเสียภาษี
