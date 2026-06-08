namespace Datacenter.Application.Features.Bank.DTOs;

// ── preview (ก่อน commit) ─────────────────────────────────────────────────────
public record BankStatementParsePreviewLineDto(
    DateTime Date, string? Description, decimal Withdrawal, decimal Deposit, decimal? Balance);

public record BankStatementParsePreviewDto(
    string BankCode, string? AccountNo, DateTime? PeriodStart, DateTime? PeriodEnd,
    decimal OpeningBalance, decimal ClosingBalance, decimal ComputedClosing,
    bool BalanceCheckPasses, string? Warning,
    string? ExpectedAccountNo, bool? AccountMatches,
    IReadOnlyList<BankStatementParsePreviewLineDto> Lines);

// ── import list ───────────────────────────────────────────────────────────────
public record BankStatementImportListDto(
    int Id, int BankAccountId, string BankCode, string? StatementAccountNo,
    DateTime PeriodStart, DateTime PeriodEnd, decimal OpeningBalance, decimal ClosingBalance,
    bool ParsedOk, int Status, int LineCount, int MatchedCount, DateTime CreatedAt, string CreatedBy,
    string? ExpectedAccountNo = null, bool? AccountMatches = null);

// ── reconciliation report ───────────────────────────────────────────────────────
public record ReconMatchedPairDto(
    int StatementLineId, DateTime Date, string? Description, decimal Amount, bool IsDeposit,
    int BankTransactionId, DateTime BookDate, string? BookCounterparty);

public record ReconStatementLineDto(
    int StatementLineId, DateTime Date, string? Description, decimal Withdrawal, decimal Deposit, decimal? Balance);

public record ReconBookTxnDto(
    int BankTransactionId, DateTime Date, string? Counterparty, string? Remark,
    decimal Deposit, decimal Withdrawal);

public record BankReconciliationDto(
    int ImportId, int ClientCompanyId, string ClientName,
    int BankAccountId, string BankAccountCode, string BankName, string BankCode,
    DateTime PeriodStart, DateTime PeriodEnd,
    decimal StatementOpeningBalance, decimal StatementClosingBalance, decimal BookClosingBalance,
    decimal ReconciledDifference, bool IsBalanced, bool ParsedOk,
    IReadOnlyList<ReconMatchedPairDto> Matched,
    IReadOnlyList<ReconStatementLineDto> UnmatchedStatement,  // ในธนาคาร ไม่อยู่ในสมุด (เช่น ค่าธรรมเนียม/ดอกเบี้ย)
    IReadOnlyList<ReconBookTxnDto> UnmatchedBook,             // ในสมุด ไม่อยู่ใน statement (deposit-in-transit/เช็คค้าง)
    DateTime? DataAsOf)
{
    public int MatchedCount => Matched.Count;
    public decimal MatchedAmount => Matched.Sum(m => m.Amount);
    public int UnmatchedStatementCount => UnmatchedStatement.Count;
    public int UnmatchedBookCount => UnmatchedBook.Count;
}
