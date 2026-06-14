using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<ClientCompany> ClientCompanies => Set<ClientCompany>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CompanyUserAccess> CompanyUserAccesses => Set<CompanyUserAccess>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<StatementLine> StatementLines => Set<StatementLine>();
    public DbSet<AccountStatementMapping> AccountStatementMappings => Set<AccountStatementMapping>();
    public DbSet<ClosingPeriod> ClosingPeriods => Set<ClosingPeriod>();
    public DbSet<ImportBatchDetail> ImportBatchDetails => Set<ImportBatchDetail>();
    public DbSet<StagingTrialBalance> StagingTrialBalances => Set<StagingTrialBalance>();
    public DbSet<StagingAccount> StagingAccounts => Set<StagingAccount>();
    public DbSet<FsExternalInput> FsExternalInputs => Set<FsExternalInput>();
    public DbSet<ComplianceTask> ComplianceTasks => Set<ComplianceTask>();
    public DbSet<ComplianceTaskTemplate> ComplianceTaskTemplates => Set<ComplianceTaskTemplate>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<AdjustmentEntry> AdjustmentEntries => Set<AdjustmentEntry>();
    public DbSet<AdjustmentEntryLine> AdjustmentEntryLines => Set<AdjustmentEntryLine>();
    public DbSet<LeaseContract> LeaseContracts => Set<LeaseContract>();
    public DbSet<AssetTypeMaster> AssetTypeMasters => Set<AssetTypeMaster>();
    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<AssetAccountMapping> AssetAccountMappings => Set<AssetAccountMapping>();
    public DbSet<VatEntry> VatEntries => Set<VatEntry>();
    public DbSet<WhtEntry> WhtEntries => Set<WhtEntry>();
    public DbSet<WhtPayee> WhtPayees => Set<WhtPayee>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ArInvoice> ArInvoices => Set<ArInvoice>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ApInvoice> ApInvoices => Set<ApInvoice>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankStatementImport> BankStatementImports => Set<BankStatementImport>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public DbSet<ReportPackage> ReportPackages => Set<ReportPackage>();
    public DbSet<NoteTemplateSection> NoteTemplateSections => Set<NoteTemplateSection>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<SsoEnrollment> SsoEnrollments => Set<SsoEnrollment>();
    public DbSet<PayrollRateConfig> PayrollRateConfigs => Set<PayrollRateConfig>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<PayrollAccountMapping> PayrollAccountMappings => Set<PayrollAccountMapping>();
    public DbSet<SsoMonthlyFiling> SsoMonthlyFilings => Set<SsoMonthlyFiling>();
    public DbSet<StatutoryFiling> StatutoryFilings => Set<StatutoryFiling>();
    public DbSet<ExpressPostingLink> ExpressPostingLinks => Set<ExpressPostingLink>();
    public DbSet<PrepaidExpense> PrepaidExpenses => Set<PrepaidExpense>();
    public DbSet<ImportSnapshot> ImportSnapshots => Set<ImportSnapshot>();
    public DbSet<ImportSnapshotFile> ImportSnapshotFiles => Set<ImportSnapshotFile>();
    public DbSet<CashCount> CashCounts => Set<CashCount>();
    public DbSet<CashCountLine> CashCountLines => Set<CashCountLine>();
    public DbSet<InterestBearingLoan> InterestBearingLoans => Set<InterestBearingLoan>();
    public DbSet<LoanPrincipalMovement> LoanPrincipalMovements => Set<LoanPrincipalMovement>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<TaxComputation> TaxComputations => Set<TaxComputation>();
    public DbSet<TaxAdjustmentLine> TaxAdjustmentLines => Set<TaxAdjustmentLine>();
    public DbSet<VatBranchMapping> VatBranchMappings => Set<VatBranchMapping>();
    public DbSet<CompanyAuditor> CompanyAuditors => Set<CompanyAuditor>();
    public DbSet<OfficeProfile> OfficeProfiles => Set<OfficeProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
