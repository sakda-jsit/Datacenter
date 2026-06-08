using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("BankStatementLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Withdrawal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Deposit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Balance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MatchStatus).HasConversion<int>();

        builder.HasIndex(x => x.BankStatementImportId);
    }
}
