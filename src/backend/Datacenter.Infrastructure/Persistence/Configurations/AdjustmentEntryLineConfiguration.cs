using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class AdjustmentEntryLineConfiguration : IEntityTypeConfiguration<AdjustmentEntryLine>
{
    public void Configure(EntityTypeBuilder<AdjustmentEntryLine> builder)
    {
        builder.ToTable("AdjustmentEntryLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DebitAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreditAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AdjustmentEntryId);
    }
}
