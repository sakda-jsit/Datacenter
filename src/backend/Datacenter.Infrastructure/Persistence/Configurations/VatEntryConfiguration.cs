using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class VatEntryConfiguration : IEntityTypeConfiguration<VatEntry>
{
    public void Configure(EntityTypeBuilder<VatEntry> builder)
    {
        builder.ToTable("VatEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VatType).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.DocumentNo).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(30);
        builder.Property(x => x.Description).HasMaxLength(120);
        builder.Property(x => x.CounterpartyTaxId).HasMaxLength(20);
        builder.Property(x => x.CounterpartyPrefix).HasMaxLength(20);
        builder.Property(x => x.RecordType).HasMaxLength(4);

        foreach (var p in new[]
                 {
                     nameof(VatEntry.BaseAmount), nameof(VatEntry.VatAmount), nameof(VatEntry.ZeroRatedAmount),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // จัดกลุ่ม ภ.พ.30 ตาม (บริษัท, เดือนภาษี, ประเภท)
        builder.HasIndex(x => new { x.ClientCompanyId, x.TaxPeriod, x.VatType });
    }
}
