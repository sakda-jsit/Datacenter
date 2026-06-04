using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class AdjustmentEntryConfiguration : IEntityTypeConfiguration<AdjustmentEntry>
{
    public void Configure(EntityTypeBuilder<AdjustmentEntry> builder)
    {
        builder.ToTable("AdjustmentEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.AdjustmentEntry)
            .HasForeignKey(l => l.AdjustmentEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // รูปแบบ query หลัก: บริษัท + ปีงบ
        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear });
        builder.HasIndex(x => new { x.ClientCompanyId, x.DocumentNo }).IsUnique();
    }
}
