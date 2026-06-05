using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable("FixedAssets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AssetName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisposalNote).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);
        builder.Property(x => x.AssetGroupCode).HasMaxLength(10);
        builder.Property(x => x.CategoryCode).HasMaxLength(10);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AccumulatedBroughtForward).HasColumnType("decimal(18,2)");

        // เงินจำนวน — decimal(18,2); อัตรา — decimal(9,4)
        foreach (var p in new[]
                 {
                     nameof(FixedAsset.Cost), nameof(FixedAsset.SalvageValue),
                     nameof(FixedAsset.DisposalProceeds),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BookRatePct).HasColumnType("decimal(9,4)");
        builder.Property(x => x.TaxRatePct).HasColumnType("decimal(9,4)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssetType)
            .WithMany()
            .HasForeignKey(x => x.AssetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.IsActive });
        builder.HasIndex(x => new { x.ClientCompanyId, x.AssetCode }).IsUnique();
    }
}
