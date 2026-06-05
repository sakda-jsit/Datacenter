using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ReportPackageConfiguration : IEntityTypeConfiguration<ReportPackage>
{
    public void Configure(EntityTypeBuilder<ReportPackage> builder)
    {
        builder.ToTable("ReportPackages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.SnapshotCompanyName).HasMaxLength(200);
        builder.Property(x => x.SnapshotTaxId).HasMaxLength(20);
        builder.Property(x => x.SnapshotBranchCode).HasMaxLength(10);
        builder.Property(x => x.SnapshotAddress).HasMaxLength(500);
        builder.Property(x => x.FinalizedBy).HasMaxLength(100);
        builder.Property(x => x.LockedBy).HasMaxLength(100);

        foreach (var p in new[]
                 {
                     nameof(ReportPackage.TotalAssets), nameof(ReportPackage.TotalLiabilities),
                     nameof(ReportPackage.TotalEquity), nameof(ReportPackage.TotalRevenue), nameof(ReportPackage.NetProfit),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear, x.Version }).IsUnique();
    }
}
