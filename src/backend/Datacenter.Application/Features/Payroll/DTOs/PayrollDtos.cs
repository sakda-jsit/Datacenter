using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Payroll.DTOs;

// ── พนักงาน ───────────────────────────────────────────────────────────────────

public record EmployeeListItemDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string NationalId,
    string? Position,
    string? Department,
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
    string? Address,
    string? Position,
    string? Department,
    string? SourceSupplierCode,
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
    string? Address,
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

// ── แมพบัญชีเงินเดือน (Express GL → ฝ่าย) สำหรับ import พนักงาน ───────────────────
public record PayrollAccountMappingDto(int Id, string AccountCode, int Role, string? Department, string? Note);
public record PayrollAccountMappingInput(string AccountCode, int Role, string? Department, string? Note);

// ── ภ.ง.ด.1ก (สรุปภาษีเงินได้หัก ณ ที่จ่ายเงินเดือนทั้งปี) ─────────────────────────
public record Pnd1kRow(
    int Seq, string NationalId, string Prefix, string FirstName, string LastName,
    string IncomeTypeCode, decimal AnnualIncome, decimal AnnualTax, int Condition);

public record Pnd1kDto(
    int Year, string CompanyName, string TaxId, string? Address,
    IReadOnlyList<Pnd1kRow> Rows, decimal TotalIncome, decimal TotalTax, int PersonCount);

// ── P6 Dashboard/Checklist + กระทบยอด 3 ทาง (รวม P3/P4/P5) ──────────────────────
/// <summary>สถานะ + กระทบยอดของหนึ่งเดือน</summary>
public record PayrollChecklistMonth(
    int Month, bool HasRun, int Status, int EmployeeCount,
    decimal TotalGross, decimal TotalNet,
    decimal SsoEmployee, decimal SsoEmployer, decimal SsoTotal, decimal Tax,
    // กระทบยอด #1 slip ↔ ระบบคำนวณ (Σ ค่าจริง − ค่าคำนวณ)
    decimal SsoCrossCheckDiff,
    // กระทบยอด #2 เงินเดือน ↔ GL (จากใบสำคัญ P4)
    bool PostingBalanced, decimal GlDiff,
    // checklist (derive จากข้อมูล)
    bool StepRecorded, bool StepBalanced, bool StepSsoReady, bool StepHasTax);

public record PayrollDashboardDto(
    int Year, int MonthsWithRun,
    IReadOnlyList<PayrollChecklistMonth> Months,
    // annual rollup
    decimal YearGross, decimal YearTax, decimal YearSsoTotal,
    // กระทบยอด #3 รายปี: ภ.ง.ด.1ก / กท.20ก / 50 ทวิ
    decimal Pnd1kTotalTax, int Pnd1kPersonCount,
    decimal Kt20Wage, int Kt20EmployeeCount, decimal Kt20Contribution,
    // ความสอดคล้อง Σ ภาษีรายเดือน == ภ.ง.ด.1ก
    bool TaxConsistent, decimal TaxConsistencyDiff);

// ── กท.20ก (แบบแสดงเงินค่าจ้างประจำปี กองทุนเงินทดแทน) ───────────────────────────
public record Kt20Row(
    int Seq, string NationalId, string Prefix, string FirstName, string LastName,
    decimal AnnualWage,   // ค่าจ้างจริงทั้งปี (ฐาน ปกส. ไม่รวม OT/วันหยุด/โบนัส)
    decimal CappedWage);  // ค่าจ้างที่ใช้คำนวณ = min(AnnualWage, เพดาน 240,000/ปี)

public record Kt20Dto(
    int Year, string CompanyName, string? Address, string? PostalCode,
    string WcfAccountNo, string WcfBranchCode,
    decimal RatePct, decimal WageCapPerYear,
    IReadOnlyList<Kt20Row> Rows,
    decimal TotalWage, int EmployeeCount, decimal Contribution, string ContributionText);

// ── ใบสำคัญลงบัญชีเงินเดือน + กระทบยอด GL ────────────────────────────────────────
/// <summary>หนึ่งบรรทัดในใบสำคัญ (Dr/Cr) + กระทบยอดกับ GL จริงเดือนนั้น</summary>
public record PayrollPostingLine(
    int Role, string RoleLabel, string? Department,
    string? AccountCode, string? AccountName, bool Mapped,
    decimal Debit, decimal Credit,
    // กระทบยอด: ความเคลื่อนไหวจริงใน GL (เดือนนั้น) ของบัญชีนี้ + ผลต่าง
    decimal GlMovement, decimal Diff);

public record PayrollPostingDto(
    int RunId, int Year, int Month, bool Balanced,
    decimal TotalDebit, decimal TotalCredit,
    IReadOnlyList<PayrollPostingLine> Lines,
    IReadOnlyList<string> Warnings);

// ── งวดเงินเดือนรายเดือน ─────────────────────────────────────────────────────────

public record PayrollRunListItemDto(
    int Id, int Year, int Month, int Status, int EmployeeCount,
    decimal TotalGross, decimal TotalSsoEmployee, decimal TotalTax, decimal TotalNet);

/// <summary>รายการต่อพนักงาน + ค่าคำนวณเทียบ (cross-check)</summary>
public record PayrollItemDto(
    int Id, int EmployeeId, string EmployeeCode, string EmployeeName, string? Department, int SalaryType,
    decimal Salary, decimal DailyWageDays, decimal DailyWageRate,
    decimal HousingAllowance, decimal FoodAllowance, decimal Overtime, decimal Diligence,
    decimal Bonus, decimal OtherIncome, decimal GrossIncome,
    decimal SsoWageBase, decimal SsoEmployee, decimal WithholdingTax,
    decimal Absence, decimal Advance, decimal OtherDeduction, decimal NetPay,
    // ── ระบบคำนวณเทียบ ──
    decimal SsoEmployeeCalc, decimal SsoEmployerCalc, decimal SsoDiff,
    decimal TaxCalc, decimal TaxDiff,
    string? Note);

public record PayrollRunDetailDto(
    int Id, int ClientCompanyId, int Year, int Month, int Status, string? Note,
    decimal? RateSsoEmployeePct, decimal? RateSsoEmployerPct, decimal? RateWageFloor, decimal? RateWageCap,
    IReadOnlyList<PayrollItemDto> Items,
    decimal TotalGross, decimal TotalSsoEmployee, decimal TotalSsoEmployer, decimal TotalTax, decimal TotalNet);

// ── สรุปรายได้ทั้งปี (แถว=เดือน, รวมทุกพนักงาน) อิง sheet "รายได้ทั้งปี" ─────────────
/// <summary>หนึ่งแถวสรุป (เดือน 1-12; Month=0 = แถวรวมทั้งปี) คอลัมน์ตรงกับ sheet รายได้ทั้งปี</summary>
public record PayrollSummaryRow(
    int Month, int EmployeeCount, bool HasRun,
    // รายได้
    decimal Salary, decimal AbsenceLate, decimal NetSalary,
    decimal Housing, decimal Food, decimal Overtime, decimal Diligence, decimal Bonus,
    decimal NetIncomeAfterAbsence, decimal TotalIncome,
    // กรอกในแบบ กท.20 ก
    decimal Wage, decimal WageOver20000,
    // รายการหัก
    decimal SsoReportable, decimal SsoCalc, decimal SsoShortfall, decimal SsoActual,
    decimal Tax, decimal Absence, decimal Advance,
    // รวม
    decimal TotalDeduction, decimal Pnd1Income, decimal EmployerSso, decimal NetPay);

public record PayrollYearSummaryDto(int Year, IReadOnlyList<PayrollSummaryRow> Months, PayrollSummaryRow Total);

// ── สปส.1-10 (แบบรายการแสดงการส่งเงินสมทบ) ─────────────────────────────────────
public record SsoFilingRow(
    int Seq, string NationalId, string Prefix, string FirstName, string LastName,
    decimal Wage, decimal Contribution);

public record SsoFilingDto(
    int RunId, int Year, int Month,
    string CompanyName, string? Address, string? PostalCode, string? Phone,
    string SsoAccountNo, string SsoBranchCode, decimal RatePct,
    IReadOnlyList<SsoFilingRow> Rows,
    decimal TotalWage, decimal TotalEmployee, decimal TotalEmployer, decimal GrandTotal,
    int InsuredCount, string GrandTotalText);

/// <summary>ค่าที่กรอกต่อรายการ (key = ItemId)</summary>
public record PayrollItemInput(
    int Id,
    decimal Salary, decimal DailyWageDays, decimal DailyWageRate,
    decimal HousingAllowance, decimal FoodAllowance, decimal Overtime, decimal Diligence,
    decimal Bonus, decimal OtherIncome,
    decimal SsoWageBase, decimal SsoEmployee, decimal WithholdingTax,
    decimal Absence, decimal Advance, decimal OtherDeduction, string? Note);
