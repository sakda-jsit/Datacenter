using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class TaxComputationConfiguration : IEntityTypeConfiguration<TaxComputation>
{
    public void Configure(EntityTypeBuilder<TaxComputation> builder)
    {
        builder.ToTable("TaxComputations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RateScheme).HasConversion<int>();
        builder.Property(x => x.CustomRatePct).HasColumnType("decimal(9,4)");
        builder.Property(x => x.LossBroughtForward).HasColumnType("decimal(18,2)");
        builder.Property(x => x.WhtCredit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.TaxComputation)
            .HasForeignKey(l => l.TaxComputationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear }).IsUnique();
    }
}

public class TaxAdjustmentLineConfiguration : IEntityTypeConfiguration<TaxAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<TaxAdjustmentLine> builder)
    {
        builder.ToTable("TaxAdjustmentLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
    }
}
