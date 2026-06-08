using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class BankStatementImportConfiguration : IEntityTypeConfiguration<BankStatementImport>
{
    public void Configure(EntityTypeBuilder<BankStatementImport> builder)
    {
        builder.ToTable("BankStatementImports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StatementAccountNo).HasMaxLength(40);
        builder.Property(x => x.SourceFileName).HasMaxLength(260);
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.SourceContent).HasColumnType("varbinary(max)");
        builder.Property(x => x.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ClosingBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.Import).HasForeignKey(l => l.BankStatementImportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClientCompanyId, x.BankAccountId, x.PeriodStart });
    }
}
