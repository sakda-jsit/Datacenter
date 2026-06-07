using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainCashCount = Datacenter.Domain.Entities.CashCount;
using Datacenter.Domain.Entities;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class CashCountConfiguration : IEntityTypeConfiguration<DomainCashCount>
{
    public void Configure(EntityTypeBuilder<DomainCashCount> builder)
    {
        builder.ToTable("CashCounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.CashCount)
            .HasForeignKey(l => l.CashCountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear, x.IsActive });
    }
}

public class CashCountLineConfiguration : IEntityTypeConfiguration<CashCountLine>
{
    public void Configure(EntityTypeBuilder<CashCountLine> builder)
    {
        builder.ToTable("CashCountLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Denomination).HasColumnType("decimal(18,2)");
        builder.Ignore(x => x.Amount); // computed
    }
}
