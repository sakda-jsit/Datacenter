using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ClosingPeriodConfiguration : IEntityTypeConfiguration<ClosingPeriod>
{
    public void Configure(EntityTypeBuilder<ClosingPeriod> builder)
    {
        builder.ToTable("ClosingPeriods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique period per company
        builder.HasIndex(x => new { x.ClientCompanyId, x.Year, x.Month }).IsUnique();
    }
}
