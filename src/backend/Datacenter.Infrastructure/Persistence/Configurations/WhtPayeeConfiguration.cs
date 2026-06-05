using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class WhtPayeeConfiguration : IEntityTypeConfiguration<WhtPayee>
{
    public void Configure(EntityTypeBuilder<WhtPayee> builder)
    {
        builder.ToTable("WhtPayees");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaxId).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.Property(x => x.Email).HasMaxLength(200);

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.TaxId }).IsUnique();
    }
}
