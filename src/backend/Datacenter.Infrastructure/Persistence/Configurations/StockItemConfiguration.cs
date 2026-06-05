using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StockCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ItemType).HasMaxLength(4);
        builder.Property(x => x.GroupCode).HasMaxLength(10);
        builder.Property(x => x.CategoryCode).HasMaxLength(10);
        builder.Property(x => x.Unit).HasMaxLength(10);

        builder.Property(x => x.OnHandQty).HasColumnType("decimal(18,4)");
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,4)");
        builder.Property(x => x.StockValue).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.StockCode }).IsUnique();
    }
}
