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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
