using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class StagingAccountConfiguration : IEntityTypeConfiguration<StagingAccount>
{
    public void Configure(EntityTypeBuilder<StagingAccount> builder)
    {
        builder.ToTable("StagingAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AccountName2).HasMaxLength(200);
        builder.Property(x => x.ParentCode).HasMaxLength(20);
        builder.Property(x => x.ValidationError).HasMaxLength(500);

        builder.HasOne(x => x.ImportBatch)
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ImportBatchId, x.AccountCode });
    }
}
