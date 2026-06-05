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
    public DbSet<ReportPackage> ReportPackages => Set<ReportPackage>();
    public DbSet<NoteTemplateSection> NoteTemplateSections => Set<NoteTemplateSection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
