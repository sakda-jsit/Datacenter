namespace Datacenter.Application.Features.Prepaid.DTOs;

// ── รายการ ──────────────────────────────────────────────────────────────────────
public record PrepaidListItemDto(
    int Id, string? Code, string Name, string? Reference,
    decimal TotalAmount, DateTime StartDate, DateTime EndDate, bool IsActive);

/// <summary>ค่าใช้จ่ายจ่ายล่วงหน้าแบบเต็ม (header + บัญชีที่ผูก)</summary>
public record PrepaidExpenseDto(
    int Id, int ClientCompanyId,
    string? Code, string Name, string? Reference,
    decimal TotalAmount, DateTime StartDate, DateTime EndDate,
    int PrepaidAccountId, string? PrepaidAccountCode,
    int ExpenseAccountId, string? ExpenseAccountCode,
    string? Notes, string? AttachmentPath, bool IsActive,
    int TotalDays);

/// <summary>ฟิลด์ที่แก้ไขได้ (ใช้ทั้ง create/update)</summary>
public record PrepaidExpenseInput(
    string? Code, string Name, string? Reference,
    decimal TotalAmount, DateTime StartDate, DateTime EndDate,
    int PrepaidAccountId, int ExpenseAccountId,
    string? Notes, string? AttachmentPath, bool IsActive);

// ── ตารางตัดจ่าย ────────────────────────────────────────────────────────────────
public record PrepaidYearDto(
    int Year, decimal OpeningAmortized, decimal Charge, decimal ClosingAmortized, decimal Remaining);

/// <summary>ยอด ณ สิ้นปีงบ: ตัดจ่ายยกมา/ตัดจ่ายในปี/ตัดจ่ายสะสม/คงเหลือ</summary>
public record PrepaidAsOfDto(
    decimal OpeningAmortized, decimal Charge, decimal ClosingAmortized, decimal Remaining, bool FullyAmortized);

public record PrepaidDetailDto(
    PrepaidExpenseDto Item, PrepaidAsOfDto AsOf, IReadOnlyList<PrepaidYearDto> Schedule);

// ── กระดาษทำการ + เทียบ GL ───────────────────────────────────────────────────────
public record PrepaidWorkpaperRowDto(
    int Id, string? Code, string Name, string? Reference,
    decimal TotalAmount, DateTime StartDate, DateTime EndDate,
    decimal OpeningAmortized, decimal ChargeInYear, decimal ClosingAmortized, decimal Remaining);

/// <summary>เทียบยอดคงเหลือตาม schedule กับยอดบัญชีค่าใช้จ่ายจ่ายล่วงหน้าใน GL (ยอดธรรมชาติเดบิต)</summary>
public record PrepaidGlCompareDto(
    int AccountId, string AccountCode, string AccountName,
    decimal ScheduleRemaining,  // คงเหลือตาม schedule (debit-positive)
    decimal GlClosing,          // ยอด GL สะสมถึงสิ้นปีงบ (debit − credit)
    decimal Diff);              // schedule − GL

public record PrepaidWorkpaperDto(
    int ClientCompanyId, string ClientCode, string ClientName, int FiscalYear,
    IReadOnlyList<PrepaidWorkpaperRowDto> Rows,
    IReadOnlyList<PrepaidGlCompareDto> GlComparison)
{
    public decimal TotalAmount => Rows.Sum(r => r.TotalAmount);
    public decimal TotalChargeInYear => Rows.Sum(r => r.ChargeInYear);
    public decimal TotalRemaining => Rows.Sum(r => r.Remaining);
    public bool HasDifference => GlComparison.Any(g => Math.Round(g.Diff, 2) != 0);
}
