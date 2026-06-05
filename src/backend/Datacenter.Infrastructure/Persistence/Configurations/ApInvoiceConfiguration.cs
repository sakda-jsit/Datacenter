using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ApInvoiceConfiguration : IEntityTypeConfiguration<ApInvoice>
{
    public void Configure(EntityTypeBuilder<ApInvoice> builder)
    {
        builder.ToTable("ApInvoices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SupplierCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SupplierName).HasMaxLength(200);
        builder.Property(x => x.Reference).HasMaxLength(40);

        foreach (var p in new[]
                 {
                     nameof(ApInvoice.Amount), nameof(ApInvoice.VatAmount), nameof(ApInvoice.NetAmount),
                     nameof(ApInvoice.PaidAmount), nameof(ApInvoice.OutstandingAmount),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatRate).HasColumnType("decimal(9,4)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.DocumentDate });
        builder.HasIndex(x => new { x.ClientCompanyId, x.SupplierCode });
    }
}
