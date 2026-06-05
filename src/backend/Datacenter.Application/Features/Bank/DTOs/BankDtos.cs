namespace Datacenter.Application.Features.Bank.DTOs;

public record BankAccountDto(
    int Id,
    string BankAccountCode,
    string BankName,
    string? Branch,
    string? AccountNumber,
    string? GlAccountCode,
    decimal BalanceForward,
    decimal CurrentBalance,   // BalanceForward + เคลื่อนไหวสุทธิทั้งหมด
    int TransactionCount);

/// <summary>หนึ่งบรรทัดในสมุดเงินฝาก (พร้อมยอดคงเหลือสะสม)</summary>
public record BankBookRowDto(
    int Id,
    DateTime TransactionDate,
    string? TransactionType,
    string? ChequeNo,
    string? CounterpartyName,
    string? Remark,
    decimal Deposit,          // เงินเข้า
    decimal Withdrawal,       // เงินออก
    decimal Balance);         // ยอดคงเหลือสะสม

public record BankBookDto(
    int ClientCompanyId,
    string ClientName,
    string BankAccountCode,
    string BankName,
    string? AccountNumber,
    int Year,
    decimal OpeningBalance,
    IReadOnlyList<BankBookRowDto> Rows,
    DateTime? DataAsOf = null)   // เวลานำเข้ารายการเดินบัญชีล่าสุด (snapshot)
{
    public decimal TotalDeposit => Rows.Sum(r => r.Deposit);
    public decimal TotalWithdrawal => Rows.Sum(r => r.Withdrawal);
    public decimal ClosingBalance => Rows.Count > 0 ? Rows[^1].Balance : OpeningBalance;
}
