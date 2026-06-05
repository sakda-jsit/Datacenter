using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SupplierCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(20);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(20);
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Phone).HasMaxLength(60);
        builder.Property(x => x.Contact).HasMaxLength(60);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.PaymentCondition).HasMaxLength(40);
        builder.Property(x => x.GlAccountCode).HasMaxLength(20);
        builder.Property(x => x.Remark).HasMaxLength(120);

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.SupplierCode }).IsUnique();
    }
}
