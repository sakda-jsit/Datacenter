using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ImportBatchDetailConfiguration : IEntityTypeConfiguration<ImportBatchDetail>
{
    public void Configure(EntityTypeBuilder<ImportBatchDetail> builder)
    {
        builder.ToTable("ImportBatchDetails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(20);
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.Property(x => x.RawData).HasMaxLength(4000);

        builder.HasOne(x => x.ImportBatch)
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ImportBatchId, x.IsValid });
    }
}
