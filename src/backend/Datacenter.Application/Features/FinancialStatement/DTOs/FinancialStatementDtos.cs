namespace Datacenter.Application.Features.FinancialStatement.DTOs;

/// <summary>One line on the financial statement (maps to a REF code).</summary>
public record FsLineDto(
    string RefCode,
    string LineName,
    char Section,
    int SortOrder,
    /// <summary>
    /// Presentation amount after sign convention applied:
    /// Assets/Expenses = debit-positive kept as-is.
    /// Liabilities/Equity/Income = flipped to show positive when credit balance.
    /// X3, X4 = shown negative (deductions).
    /// </summary>
    decimal Amount,
    /// <summary>Account codes + names that contribute to this line (for drill-down).</summary>
    IReadOnlyList<FsLineAccountDto> Accounts);

public record FsLineAccountDto(
    string AccountCode,
    string AccountName,
    decimal NetBalance);

/// <summary>Balance Sheet: Assets, Liabilities, Equity.</summary>
public record BalanceSheetDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int FiscalYear,
    IReadOnlyList<FsLineDto> Assets,
    IReadOnlyList<FsLineDto> Liabilities,
    IReadOnlyList<FsLineDto> Equity,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity,
    /// <summary>TotalAssets - TotalLiabilitiesAndEquity; should be 0.</summary>
    decimal BalanceDifference);

/// <summary>Profit and Loss: Income, COGS, Expenses, derived subtotals.</summary>
public record ProfitLossDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int FiscalYear,
    int? MonthFrom,
    int? MonthTo,
    IReadOnlyList<FsLineDto> IncomeLines,
    FsLineDto CostOfGoods,
    IReadOnlyList<FsLineDto> ExpenseLines,
    FsLineDto FinanceCost,
    FsLineDto IncomeTax,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal GrossProfit,
    decimal ProfitBeforeFinance,
    decimal ProfitBeforeTax,
    decimal NetProfit);

public record AccountMappingDto(
    string AccountCode,
    string AccountName,
    string RefCode,
    string LineName,
    char Section);

public record FsExternalInputDto(
    int Id,
    int FiscalYear,
    string RefCode,
    decimal Amount,
    string? Note);
