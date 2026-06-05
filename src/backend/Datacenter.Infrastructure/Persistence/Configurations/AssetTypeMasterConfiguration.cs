using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class AssetTypeMasterConfiguration : IEntityTypeConfiguration<AssetTypeMaster>
{
    public void Configure(EntityTypeBuilder<AssetTypeMaster> builder)
    {
        builder.ToTable("AssetTypeMasters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DefaultBookRatePct).HasColumnType("decimal(9,4)");
        builder.Property(x => x.DefaultTaxRatePct).HasColumnType("decimal(9,4)");

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
