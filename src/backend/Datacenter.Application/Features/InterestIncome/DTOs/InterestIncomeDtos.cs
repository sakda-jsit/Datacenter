namespace Datacenter.Application.Features.InterestIncome.DTOs;

// ── movement ─────────────────────────────────────────────────────────────────────
public record LoanMovementInput(DateTime Date, decimal Amount, string? Description);
public record LoanMovementDto(DateTime Date, decimal Amount, string? Description);

// ── รายการ ──────────────────────────────────────────────────────────────────────
public record InterestLoanListItemDto(
    int Id, string Name, string? Reference, decimal AnnualRatePct, bool IsActive);

/// <summary>ฟิลด์ที่แก้ไขได้ (ใช้ทั้ง create/update)</summary>
public record InterestLoanInput(
    string Name, string? Reference,
    decimal AnnualRatePct, decimal SbtRatePct, decimal LocalTaxPctOfSbt, int DayCountBasis,
    int InterestReceivableAccountId, int InterestIncomeAccountId,
    string? Notes, string? AttachmentPath, bool IsActive,
    IReadOnlyList<LoanMovementInput> Movements);

public record InterestLoanDto(
    int Id, int ClientCompanyId, string Name, string? Reference,
    decimal AnnualRatePct, decimal SbtRatePct, decimal LocalTaxPctOfSbt, int DayCountBasis,
    int InterestReceivableAccountId, string? InterestReceivableAccountCode,
    int InterestIncomeAccountId, string? InterestIncomeAccountCode,
    string? Notes, string? AttachmentPath, bool IsActive,
    IReadOnlyList<LoanMovementDto> Movements);

// ── schedule (ช่วงดอกเบี้ยภายในปีงบ) ────────────────────────────────────────────────
public record InterestSegmentDto(
    DateTime FromDate, DateTime ToDate, decimal Balance, int Days, decimal Interest);

/// <summary>ยอด ณ ปีงบ: เงินต้นต้นปี/ปลายปี, ดอกเบี้ยรับในปี, ภาษีธุรกิจเฉพาะ, ส่วนท้องถิ่น, รวมภาษี</summary>
public record InterestAsOfDto(
    decimal OpeningBalance, decimal ClosingBalance,
    decimal InterestForYear, decimal Sbt, decimal LocalTax, decimal TotalTax);

public record InterestLoanDetailDto(
    InterestLoanDto Item, InterestAsOfDto AsOf, IReadOnlyList<InterestSegmentDto> Segments);

// ── กระดาษทำการ + เทียบ GL ───────────────────────────────────────────────────────
public record InterestWorkpaperRowDto(
    int Id, string Name, string? Reference, decimal AnnualRatePct,
    decimal OpeningBalance, decimal ClosingBalance,
    decimal InterestForYear, decimal Sbt, decimal LocalTax);

/// <summary>เทียบดอกเบี้ยรับที่คำนวณ กับ movement บัญชีรายได้ดอกเบี้ยใน GL ในปีงบ (credit − debit)</summary>
public record InterestGlCompareDto(
    int AccountId, string AccountCode, string AccountName,
    decimal ScheduleInterest,  // ดอกเบี้ยที่คำนวณรวมตามบัญชีรายได้
    decimal GlMovement,        // movement ในปี (credit − debit)
    decimal Diff);             // schedule − GL

public record InterestWorkpaperDto(
    int ClientCompanyId, string ClientCode, string ClientName, int FiscalYear,
    IReadOnlyList<InterestWorkpaperRowDto> Rows,
    IReadOnlyList<InterestGlCompareDto> GlComparison)
{
    public decimal TotalInterest => Rows.Sum(r => r.InterestForYear);
    public decimal TotalSbt => Rows.Sum(r => r.Sbt);
    public decimal TotalLocalTax => Rows.Sum(r => r.LocalTax);
    public bool HasDifference => GlComparison.Any(g => Math.Round(g.Diff, 2) != 0);
}
