using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class AssetAccountMappingConfiguration : IEntityTypeConfiguration<AssetAccountMapping>
{
    public void Configure(EntityTypeBuilder<AssetAccountMapping> builder)
    {
        builder.ToTable("AssetAccountMappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CategoryCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(100);

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.CategoryCode }).IsUnique();
    }
}
