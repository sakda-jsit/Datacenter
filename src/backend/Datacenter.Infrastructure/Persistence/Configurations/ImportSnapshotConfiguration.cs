using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ImportSnapshotConfiguration : IEntityTypeConfiguration<ImportSnapshot>
{
    public void Configure(EntityTypeBuilder<ImportSnapshot> builder)
    {
        builder.ToTable("ImportSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceFolderPath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ArchiveRelativePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ArchiveFileName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ArchiveSha256).HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>();

        // 1:1 กับ ImportBatch — ลบ batch แล้ว snapshot row ลบตาม (ไฟล์ zip ลบใน handler)
        builder.HasOne(x => x.ImportBatch)
            .WithMany()
            .HasForeignKey(x => x.ImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ImportBatchId).IsUnique();

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Files)
            .WithOne(f => f.ImportSnapshot)
            .HasForeignKey(f => f.ImportSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear });
    }
}

public class ImportSnapshotFileConfiguration : IEntityTypeConfiguration<ImportSnapshotFile>
{
    public void Configure(EntityTypeBuilder<ImportSnapshotFile> builder)
    {
        builder.ToTable("ImportSnapshotFiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TableName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64);
    }
}
