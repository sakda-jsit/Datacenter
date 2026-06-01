namespace Datacenter.Application.Features.GeneralLedger.DTOs;

public record GeneralLedgerLineDto(
    int JournalEntryId,
    string DocumentNo,
    DateTime JournalDate,
    string Description,
    string SourceModule,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal RunningBalance);

public record GeneralLedgerAccountDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    IReadOnlyList<GeneralLedgerLineDto> Lines);

public record GeneralLedgerReportDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int Year,
    int? MonthFrom,
    int? MonthTo,
    IReadOnlyList<GeneralLedgerAccountDto> Accounts);
