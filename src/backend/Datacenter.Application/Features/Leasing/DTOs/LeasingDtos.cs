using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Leasing.DTOs;

// ── Contract ──────────────────────────────────────────────────────────────────

/// <summary>หนึ่งบรรทัดในรายการสัญญา (สรุป)</summary>
public record LeaseContractListItemDto(
    int Id,
    LeaseContractType ContractType,
    string ContractNo,
    string AssetName,
    string? AssetCode,
    string? Lessor,
    DateTime FirstInstallmentDate,
    int NumberOfPeriods,
    decimal FinancedPrincipal,
    decimal InstallmentAmount,
    bool IsActive);

/// <summary>สัญญาแบบเต็ม (header + บัญชีที่ผูก + ยอดรวมที่คำนวณได้)</summary>
public record LeaseContractDto(
    int Id,
    int ClientCompanyId,
    LeaseContractType ContractType,
    string ContractNo,
    string AssetName,
    string? AssetCode,
    string? Lessor,
    DateTime ContractDate,
    DateTime FirstInstallmentDate,
    int NumberOfPeriods,
    int PaymentsPerYear,
    decimal CashPrice,
    decimal DownPayment,
    decimal FinancedPrincipal,
    decimal InstallmentAmount,
    decimal VatPerPeriod,
    int LiabilityAccountId,
    string? LiabilityAccountCode,
    int? DeferredInterestAccountId,
    string? DeferredInterestAccountCode,
    int? InputVatUndueAccountId,
    string? InputVatUndueAccountCode,
    int InterestExpenseAccountId,
    string? InterestExpenseAccountCode,
    string? Notes,
    string? AttachmentPath,
    bool IsActive,
    // ── ยอดรวมที่คำนวณจาก engine ──
    decimal TotalInterest,
    decimal TotalVat,
    decimal GrossLiabilityTotal,
    decimal EffectiveRatePerPeriod);

/// <summary>ฟิลด์ที่แก้ไขได้ของสัญญา (ใช้ทั้ง create/update)</summary>
public record LeaseContractInput(
    LeaseContractType ContractType,
    string ContractNo,
    string AssetName,
    string? AssetCode,
    string? Lessor,
    DateTime ContractDate,
    DateTime FirstInstallmentDate,
    int NumberOfPeriods,
    int PaymentsPerYear,
    decimal CashPrice,
    decimal DownPayment,
    decimal FinancedPrincipal,
    decimal InstallmentAmount,
    decimal VatPerPeriod,
    int LiabilityAccountId,
    int? DeferredInterestAccountId,
    int? InputVatUndueAccountId,
    int InterestExpenseAccountId,
    string? Notes,
    string? AttachmentPath,
    bool IsActive);

// ── Amortization schedule ───────────────────────────────────────────────────

public record LeaseSchedulePeriodDto(
    int PeriodNo,
    DateTime DueDate,
    decimal Installment,           // net of VAT (principal + interest)
    decimal Principal,
    decimal Interest,
    decimal Vat,
    decimal GrossInstallment,      // installment + VAT (ยอดผ่อนจริงต่องวด)
    decimal ClosingNetPrincipal,
    decimal ClosingDeferredInterest,
    decimal ClosingVatUndue,
    decimal ClosingGrossLiability);

// ── Year-end breakdown (ต่อองค์ประกอบ) ────────────────────────────────────────

/// <summary>ยอดยกมา / ชำระในปี / ยอดคงเหลือ (= current + long-term) ของหนึ่งองค์ประกอบ ณ สิ้นปีงบ</summary>
public record LeaseAccountBreakdownDto(
    decimal Opening,
    decimal PaidInYear,
    decimal Closing,
    decimal CurrentPortion,
    decimal LongTerm);

public record LeaseYearEndSummaryDto(
    int FiscalYear,
    LeaseAccountBreakdownDto GrossLiability,
    LeaseAccountBreakdownDto DeferredInterest,
    LeaseAccountBreakdownDto VatUndue,
    LeaseAccountBreakdownDto NetPrincipal,
    decimal InterestRecognizedInYear);

/// <summary>รายละเอียดสัญญา + ตารางตัดบัญชี + สรุปสิ้นปีของปีงบที่ขอ</summary>
public record LeaseContractDetailDto(
    LeaseContractDto Contract,
    LeaseYearEndSummaryDto YearEnd,
    IReadOnlyList<LeaseSchedulePeriodDto> Schedule);

// ── Workpaper (sheet SUM) + GL comparison ─────────────────────────────────────

public record LeaseWorkpaperRowDto(
    int ContractId,
    LeaseContractType ContractType,
    string ContractNo,
    string AssetName,
    string? AssetCode,
    string? Lessor,
    LeaseAccountBreakdownDto GrossLiability,
    LeaseAccountBreakdownDto DeferredInterest,
    LeaseAccountBreakdownDto VatUndue,
    decimal InterestRecognizedInYear);

/// <summary>เทียบยอด schedule รวม กับยอด GL ของบัญชีที่ผูก (ผลต่าง = ฐานของ adjustment)</summary>
public record LeaseGlCompareDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    string Role,                 // Liability / DeferredInterest / VatUndue
    decimal ScheduleClosing,     // ยอดคงเหลือตาม schedule (credit-positive สำหรับหนี้สิน, debit-positive สำหรับ contra/asset)
    decimal GlClosing,           // ยอดใน GL (sign เดียวกัน)
    decimal Diff);               // schedule − GL

public record LeaseWorkpaperDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int FiscalYear,
    IReadOnlyList<LeaseWorkpaperRowDto> Rows,
    IReadOnlyList<LeaseGlCompareDto> GlComparison)
{
    public decimal TotalGrossLiabilityClosing => Rows.Sum(r => r.GrossLiability.Closing);
    public decimal TotalDeferredInterestClosing => Rows.Sum(r => r.DeferredInterest.Closing);
    public decimal TotalVatUndueClosing => Rows.Sum(r => r.VatUndue.Closing);
    public decimal TotalInterestRecognized => Rows.Sum(r => r.InterestRecognizedInYear);
    public bool HasDifference => GlComparison.Any(g => Math.Round(g.Diff, 2) != 0);
}
