using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.TrialBalance.DTOs;

/// <summary>
/// One row in the trial balance report.
/// Net = DebitTotal - CreditTotal (positive = debit balance, negative = credit balance).
/// </summary>
public record TrialBalanceRowDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    int Level,
    string? ParentCode,
    decimal BeginDebit,
    decimal BeginCredit,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal EndDebit,
    decimal EndCredit)
{
    public decimal BeginNet => BeginDebit - BeginCredit;
    public decimal PeriodNet => PeriodDebit - PeriodCredit;
    public decimal EndNet => EndDebit - EndCredit;
}

public record TrialBalanceReportDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int Year,
    int? MonthFrom,
    int? MonthTo,
    IReadOnlyList<TrialBalanceRowDto> Rows)
{
    public decimal TotalBeginDebit => Rows.Sum(r => r.BeginDebit);
    public decimal TotalBeginCredit => Rows.Sum(r => r.BeginCredit);
    public decimal TotalPeriodDebit => Rows.Sum(r => r.PeriodDebit);
    public decimal TotalPeriodCredit => Rows.Sum(r => r.PeriodCredit);
    public decimal TotalEndDebit => Rows.Sum(r => r.EndDebit);
    public decimal TotalEndCredit => Rows.Sum(r => r.EndCredit);
}

public record AccountListDto(
    int Id,
    string AccountCode,
    string AccountName,
    string? AccountName2,
    AccountType AccountType,
    int Level,
    string? ParentCode,
    bool IsPostable,
    bool IsActive);

public record PeriodStatusDto(
    int Year,
    int Month,
    PeriodStatus Status,
    DateTime? ClosedAt);
