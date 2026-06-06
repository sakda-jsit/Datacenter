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
    int? ClientCompanyId,   // null = ค่ากลาง
    bool IsGlobal,
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
