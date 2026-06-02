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
    int YearThai);

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
