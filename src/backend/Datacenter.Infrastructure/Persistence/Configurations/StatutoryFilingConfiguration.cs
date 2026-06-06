using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class StatutoryFilingConfiguration : IEntityTypeConfiguration<StatutoryFiling>
{
    public void Configure(EntityTypeBuilder<StatutoryFiling> builder)
    {
        builder.ToTable("StatutoryFilings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FilingType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ReceiptNo).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.FormFileName).HasMaxLength(260);
        builder.Property(x => x.FormContentType).HasMaxLength(120);
        builder.Property(x => x.ReceiptFileName).HasMaxLength(260);
        builder.Property(x => x.ReceiptContentType).HasMaxLength(120);

        foreach (var p in new[]
                 {
                     nameof(StatutoryFiling.SnapshotBase), nameof(StatutoryFiling.SnapshotAmount),
                     nameof(StatutoryFiling.ReceiptAmount),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasOne<ClientCompany>()
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FilingType, x.Year, x.Month }).IsUnique();
    }
}
