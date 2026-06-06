using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Payroll.DTOs;

// ── พนักงาน ───────────────────────────────────────────────────────────────────

public record EmployeeListItemDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string NationalId,
    string? Position,
    DateTime StartDate,
    DateTime? ResignDate,
    EmploymentStatus EmploymentStatus,
    SsoMemberStatus SsoStatus,
    decimal BaseSalary);

public record EmployeeDocumentDto(
    int Id,
    EmployeeDocType DocType,
    string FileName,
    string ContentType,
    DateTime? EffectiveDate,
    string? Note,
    DateTime UploadedAt,
    string UploadedBy);

public record SsoEnrollmentDto(
    int Id,
    SsoEnrollmentType Type,
    DateTime EventDate,
    DateTime? SubmittedDate,
    SsoEnrollmentStatus Status,
    int? ProofDocumentId,
    string? Note);

public record EmployeeDetailDto(
    int Id,
    int ClientCompanyId,
    string EmployeeCode,
    string NationalId,
    string? Prefix,
    string FirstName,
    string LastName,
    DateTime? BirthDate,
    string? MaritalStatus,
    string? Nationality,
    string? Position,
    string? Department,
    DateTime StartDate,
    DateTime? ResignDate,
    EmploymentStatus EmploymentStatus,
    SalaryType SalaryType,
    decimal BaseSalary,
    decimal? DailyWage,
    string? SsoNumber,
    string? SsoHospital,
    SsoMemberStatus SsoStatus,
    string? TaxId,
    string? Note,
    IReadOnlyList<EmployeeDocumentDto> Documents,
    IReadOnlyList<SsoEnrollmentDto> SsoEnrollments);

/// <summary>ฟิลด์สำหรับสร้าง/แก้ไขพนักงาน (กรอกมือ)</summary>
public record EmployeeInput(
    string EmployeeCode,
    string NationalId,
    string? Prefix,
    string FirstName,
    string LastName,
    DateTime? BirthDate,
    string? MaritalStatus,
    string? Nationality,
    string? Position,
    string? Department,
    DateTime StartDate,
    DateTime? ResignDate,
    EmploymentStatus EmploymentStatus,
    SalaryType SalaryType,
    decimal BaseSalary,
    decimal? DailyWage,
    string? SsoNumber,
    string? SsoHospital,
    SsoMemberStatus SsoStatus,
    string? TaxId,
    string? Note);

/// <summary>เนื้อไฟล์เอกสาร (สำหรับดาวน์โหลด — มี audit PDPA)</summary>
public record EmployeeDocumentContentDto(string FileName, string ContentType, byte[] Content);

// ── อัตราเงินสมทบ ปกส./กองทุนทดแทน (effective-dated) ───────────────────────────

public record PayrollRateConfigDto(
    int Id,
    DateTime EffectiveFrom,
    decimal SsoEmployeePct,
    decimal SsoEmployerPct,
    decimal SsoWageFloor,
    decimal SsoWageCap,
    decimal WcfRatePct,
    decimal WcfWageCapPerYear,
    string? Note);

public record PayrollRateConfigInput(
    DateTime EffectiveFrom,
    decimal SsoEmployeePct,
    decimal SsoEmployerPct,
    decimal SsoWageFloor,
    decimal SsoWageCap,
    decimal WcfRatePct,
    decimal WcfWageCapPerYear,
    string? Note);

// ── งวดเงินเดือนรายเดือน ─────────────────────────────────────────────────────────

public record PayrollRunListItemDto(
    int Id, int Year, int Month, int Status, int EmployeeCount,
    decimal TotalGross, decimal TotalSsoEmployee, decimal TotalTax, decimal TotalNet);

/// <summary>รายการต่อพนักงาน + ค่าคำนวณเทียบ (cross-check)</summary>
public record PayrollItemDto(
    int Id, int EmployeeId, string EmployeeCode, string EmployeeName, int SalaryType,
    decimal Salary, decimal DailyWageDays, decimal DailyWageRate,
    decimal HousingAllowance, decimal FoodAllowance, decimal Overtime, decimal Diligence,
    decimal Bonus, decimal OtherIncome, decimal GrossIncome,
    decimal SsoWageBase, decimal SsoEmployee, decimal WithholdingTax,
    decimal Absence, decimal OtherDeduction, decimal NetPay,
    // ── ระบบคำนวณเทียบ ──
    decimal SsoEmployeeCalc, decimal SsoEmployerCalc, decimal SsoDiff,
    decimal TaxCalc, decimal TaxDiff,
    string? Note);

public record PayrollRunDetailDto(
    int Id, int ClientCompanyId, int Year, int Month, int Status, string? Note,
    decimal? RateSsoEmployeePct, decimal? RateSsoEmployerPct, decimal? RateWageFloor, decimal? RateWageCap,
    IReadOnlyList<PayrollItemDto> Items,
    decimal TotalGross, decimal TotalSsoEmployee, decimal TotalSsoEmployer, decimal TotalTax, decimal TotalNet);

/// <summary>ค่าที่กรอกต่อรายการ (key = ItemId)</summary>
public record PayrollItemInput(
    int Id,
    decimal Salary, decimal DailyWageDays, decimal DailyWageRate,
    decimal HousingAllowance, decimal FoodAllowance, decimal Overtime, decimal Diligence,
    decimal Bonus, decimal OtherIncome,
    decimal SsoWageBase, decimal SsoEmployee, decimal WithholdingTax,
    decimal Absence, decimal OtherDeduction, string? Note);
