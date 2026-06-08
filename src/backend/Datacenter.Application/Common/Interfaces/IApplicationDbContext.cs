using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ClientCompany> ClientCompanies { get; }
    DbSet<User> Users { get; }
    DbSet<CompanyUserAccess> CompanyUserAccesses { get; }
    DbSet<Account> Accounts { get; }
    DbSet<ImportBatch> ImportBatches { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }
    DbSet<StatementLine> StatementLines { get; }
    DbSet<AccountStatementMapping> AccountStatementMappings { get; }
    DbSet<ClosingPeriod> ClosingPeriods { get; }
    DbSet<ImportBatchDetail> ImportBatchDetails { get; }
    DbSet<StagingTrialBalance> StagingTrialBalances { get; }
    DbSet<StagingAccount> StagingAccounts { get; }
    DbSet<FsExternalInput> FsExternalInputs { get; }
    DbSet<ComplianceTask> ComplianceTasks { get; }
    DbSet<ComplianceTaskTemplate> ComplianceTaskTemplates { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AccountingPeriod> AccountingPeriods { get; }
    DbSet<AdjustmentEntry> AdjustmentEntries { get; }
    DbSet<AdjustmentEntryLine> AdjustmentEntryLines { get; }
    DbSet<LeaseContract> LeaseContracts { get; }
    DbSet<AssetTypeMaster> AssetTypeMasters { get; }
    DbSet<FixedAsset> FixedAssets { get; }
    DbSet<AssetAccountMapping> AssetAccountMappings { get; }
    DbSet<VatEntry> VatEntries { get; }
    DbSet<WhtEntry> WhtEntries { get; }
    DbSet<WhtPayee> WhtPayees { get; }
    DbSet<Customer> Customers { get; }
    DbSet<ArInvoice> ArInvoices { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<ApInvoice> ApInvoices { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankTransaction> BankTransactions { get; }
    DbSet<BankStatementImport> BankStatementImports { get; }
    DbSet<BankStatementLine> BankStatementLines { get; }
    DbSet<ReportPackage> ReportPackages { get; }
    DbSet<NoteTemplateSection> NoteTemplateSections { get; }
    DbSet<Employee> Employees { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<SsoEnrollment> SsoEnrollments { get; }
    DbSet<PayrollRateConfig> PayrollRateConfigs { get; }
    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollItem> PayrollItems { get; }
    DbSet<PayrollAccountMapping> PayrollAccountMappings { get; }
    DbSet<SsoMonthlyFiling> SsoMonthlyFilings { get; }
    DbSet<StatutoryFiling> StatutoryFilings { get; }
    DbSet<ExpressPostingLink> ExpressPostingLinks { get; }
    DbSet<PrepaidExpense> PrepaidExpenses { get; }
    DbSet<ImportSnapshot> ImportSnapshots { get; }
    DbSet<ImportSnapshotFile> ImportSnapshotFiles { get; }
    DbSet<CashCount> CashCounts { get; }
    DbSet<CashCountLine> CashCountLines { get; }
    DbSet<InterestBearingLoan> InterestBearingLoans { get; }
    DbSet<LoanPrincipalMovement> LoanPrincipalMovements { get; }
    DbSet<Attachment> Attachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
