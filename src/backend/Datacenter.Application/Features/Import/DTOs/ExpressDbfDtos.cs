namespace Datacenter.Application.Features.Import.DTOs;

/// <summary>
/// หนึ่งรายการในทะเบียนข้อมูลของ Express (J:\secure\sccomp.dbf)
/// CompName = "ชื่อข้อมูล" (อาจมี prefix X-/Z- กำกับรายการเก่า/สำเนา)
/// Path     = ชื่อโฟลเดอร์บริษัทใต้ ExpressBasePath
/// Candel   = ค่าธงจากคอลัมน์ CANDEL ('Y'/'N')
/// </summary>
public record ExpressDatasetDto(
    string CompName,
    string Path,
    string Candel);

public record ExpressCompanyInfoDto(
    string ThaiName,
    string EngName,
    string TaxId,
    decimal VatRate,
    int YearThai,
    string? Address = null);

public record ExpressAccountRowDto(
    string AccountCode,
    string AccountName,
    string? AccountName2,
    int Level,
    string? ParentCode,
    int Group,
    int AccountType);

/// <summary>
/// One row per (AccountCode, PeriodSet) combination.
/// PeriodSet: "LY" | "CUR" | "NY"
/// BeginBalance, TotalDebit, TotalCredit are already aggregated from monthly columns.
/// ClosingDebit/ClosingCredit non-zero only for PeriodSet="CUR".
/// EndBalance = BeginBalance + TotalDebit - TotalCredit +/- closing adjustments.
/// </summary>
public record ExpressTrialBalanceRowDto(
    string AccountCode,
    string PeriodSet,
    decimal BeginBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingDebit,
    decimal ClosingCredit,
    decimal EndBalance);

/// <summary>
/// หนึ่งงวดในนิยามรอบบัญชีจาก ISPRD
/// Locked = true เมื่อ Express ระบุ LOCK='Y' (งวดถูกปิด/ล็อกแล้วในต้นทาง)
/// </summary>
public record ExpressAccountingPeriodDto(
    DateTime BeginDate,
    DateTime EndDate,
    bool Locked);

/// <summary>
/// หนึ่งสินทรัพย์จากทะเบียนสินทรัพย์ Express (FAMAS.DBF).
/// AccumulatedBroughtForward (ACCMBF) = ค่าเสื่อมสะสมยกมาต้นปีปัจจุบันของไฟล์;
/// Method 1 = เส้นตรง; SaleDate/SaleAmount มีค่าเมื่อจำหน่าย/ขาย.
/// </summary>
public record ExpressFixedAssetDto(
    string AssetCode,
    string AssetName,
    string? GroupCode,
    string? CategoryCode,
    DateTime? AcquireDate,
    decimal Cost,
    decimal Salvage,
    decimal RatePct,
    int LifeYears,
    string Method,
    decimal AccumulatedBroughtForward,
    DateTime? SaleDate,
    decimal SaleAmount,
    string Status);

/// <summary>
/// หนึ่งรายการในรายงานภาษีมูลค่าเพิ่มจาก Express ISVAT.DBF.
/// VatRecType: "S" = ภาษีขาย (Output), "P" = ภาษีซื้อ (Input).
/// TaxPeriod (VATPRD) = เดือนภาษี; BaseAmount = AMT01+AMT02; VatAmount = VAT01+VAT02; ZeroRated = AMTRAT0.
/// </summary>
public record ExpressVatEntryDto(
    string VatRecType,
    DateTime? TaxPeriod,
    DateTime? DocumentDate,
    DateTime? VatDate,
    string DocumentNo,
    string? ReferenceNo,
    string? Description,
    string? CounterpartyTaxId,
    string? CounterpartyPrefix,
    decimal BaseAmount,
    decimal VatAmount,
    decimal ZeroRatedAmount,
    bool IsLate,
    string? RecordType);

/// <summary>
/// หนึ่งรายการภาษีหัก ณ ที่จ่ายจาก Express ISTAX.DBF.
/// FormTypeCode: "S03" = ภ.ง.ด.3 (บุคคลธรรมดา), "S53" = ภ.ง.ด.53 (นิติบุคคล).
/// หนึ่งระเบียน ISTAX อาจมี 2 ชุดเงินได้ (ชุดหลัก + ...2) → adapter แตกเป็น 2 DTO เมื่อชุด 2 มีค่า.
/// </summary>
/// <summary>ลูกค้าจาก Express ARMAS.DBF</summary>
public record ExpressCustomerDto(
    string CustomerCode,
    string? Prefix,
    string Name,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Contact,
    string? Email,
    int PaymentTermDays,
    string? PaymentCondition,
    string? GlAccountCode,
    string? Remark,
    bool IsActive);

/// <summary>ใบแจ้งหนี้ลูกหนี้จาก Express ARTRN.DBF (RECTYP='3')</summary>
public record ExpressArInvoiceDto(
    string DocumentNo,
    DateTime DocumentDate,
    DateTime? DueDate,
    string CustomerCode,
    decimal Amount,
    decimal VatRate,
    decimal VatAmount,
    decimal NetAmount,
    decimal ReceivedAmount,
    decimal OutstandingAmount,
    bool IsCompleted,
    DateTime? VatPeriod,
    string? Reference);

/// <summary>ผู้ขายจาก Express APMAS.DBF</summary>
public record ExpressSupplierDto(
    string SupplierCode,
    string? Prefix,
    string Name,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Contact,
    string? Email,
    int PaymentTermDays,
    string? PaymentCondition,
    string? GlAccountCode,
    string? Remark,
    bool IsActive);

/// <summary>ใบตั้งหนี้เจ้าหนี้จาก Express APTRN.DBF (RECTYP='3')</summary>
public record ExpressApInvoiceDto(
    string DocumentNo,
    DateTime DocumentDate,
    DateTime? DueDate,
    string SupplierCode,
    decimal Amount,
    decimal VatRate,
    decimal VatAmount,
    decimal NetAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool IsCompleted,
    DateTime? VatPeriod,
    string? Reference);

public record ExpressWhtEntryDto(
    string SourceKey,
    string FormTypeCode,
    DateTime? TaxPeriod,
    DateTime? WithholdDate,
    string DocumentNo,
    string? ReferenceNo,
    string? PayeeName,
    string? PayeePrefix,
    string? PayeeTaxId,
    string? PayeeAddress,
    string? IncomeType,
    decimal BaseAmount,
    decimal TaxRate,
    decimal TaxAmount,
    string? Condition,
    bool IsLate);
