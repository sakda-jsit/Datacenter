using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Adjustments.DTOs;

// ── Adjustment entry (header + lines) ─────────────────────────────────────────

public record AdjustmentLineDto(
    int Id,
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description);

public record AdjustmentEntryDto(
    int Id,
    int ClientCompanyId,
    int FiscalYear,
    string DocumentNo,
    DateTime EntryDate,
    AdjustmentSourceType SourceType,
    string? Reference,
    string Reason,
    string? AttachmentPath,
    string? CreatedBy,
    DateTime CreatedAt,
    IReadOnlyList<AdjustmentLineDto> Lines)
{
    public decimal TotalDebit => Lines.Sum(l => l.DebitAmount);
    public decimal TotalCredit => Lines.Sum(l => l.CreditAmount);
}

/// <summary>บรรทัดที่ส่งเข้ามาตอนสร้าง/แก้ไข adjustment</summary>
public record AdjustmentLineInput(
    int AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description);

// ── Adjusted trial balance (5 column groups, docs/13 ข้อ 1) ────────────────────

public record AdjustedTrialBalanceRowDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    int Level,
    string? ParentCode,
    // ยอดยกมา
    decimal BeginDebit,
    decimal BeginCredit,
    // เคลื่อนไหวระหว่างงวด
    decimal MovementDebit,
    decimal MovementCredit,
    // ยอดคงเหลือก่อนปรับปรุง (net-presented)
    decimal BalanceBeforeDebit,
    decimal BalanceBeforeCredit,
    // รายการปรับปรุง
    decimal AdjustmentDebit,
    decimal AdjustmentCredit,
    // ยอดหลังปรับปรุง (net-presented)
    decimal FinalDebit,
    decimal FinalCredit);

public record AdjustedTrialBalanceReportDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int FiscalYear,
    IReadOnlyList<AdjustedTrialBalanceRowDto> Rows)
{
    public decimal TotalBalanceBeforeDebit => Rows.Sum(r => r.BalanceBeforeDebit);
    public decimal TotalBalanceBeforeCredit => Rows.Sum(r => r.BalanceBeforeCredit);
    public decimal TotalAdjustmentDebit => Rows.Sum(r => r.AdjustmentDebit);
    public decimal TotalAdjustmentCredit => Rows.Sum(r => r.AdjustmentCredit);
    public decimal TotalFinalDebit => Rows.Sum(r => r.FinalDebit);
    public decimal TotalFinalCredit => Rows.Sum(r => r.FinalCredit);

    /// <summary>ยอดก่อนปรับสมดุล (debit = credit)</summary>
    public bool BalancedBefore => Math.Round(TotalBalanceBeforeDebit - TotalBalanceBeforeCredit, 2) == 0;
    /// <summary>รายการปรับปรุงสมดุล</summary>
    public bool AdjustmentsBalanced => Math.Round(TotalAdjustmentDebit - TotalAdjustmentCredit, 2) == 0;
    /// <summary>ยอดหลังปรับสมดุล</summary>
    public bool BalancedAfter => Math.Round(TotalFinalDebit - TotalFinalCredit, 2) == 0;
}
