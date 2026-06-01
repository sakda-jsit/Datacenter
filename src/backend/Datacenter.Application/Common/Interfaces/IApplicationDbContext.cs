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
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
