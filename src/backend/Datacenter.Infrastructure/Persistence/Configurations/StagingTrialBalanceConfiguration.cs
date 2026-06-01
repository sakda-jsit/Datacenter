using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class StagingTrialBalanceConfiguration : IEntityTypeConfiguration<StagingTrialBalance>
{
    public void Configure(EntityTypeBuilder<StagingTrialBalance> builder)
    {
        builder.ToTable("StagingTrialBalances");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccountName).HasMaxLength(200);
        builder.Property(x => x.PeriodSet).HasMaxLength(3).IsRequired();
        builder.Property(x => x.BeginBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalDebit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalCredit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ClosingDebit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ClosingCredit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EndBalance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ValidationError).HasMaxLength(500);

        builder.HasOne(x => x.ImportBatch)
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ImportBatchId, x.PeriodSet, x.AccountCode });
    }
}
