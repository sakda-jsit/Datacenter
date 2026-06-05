using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ArInvoiceConfiguration : IEntityTypeConfiguration<ArInvoice>
{
    public void Configure(EntityTypeBuilder<ArInvoice> builder)
    {
        builder.ToTable("ArInvoices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentNo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CustomerCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.Reference).HasMaxLength(40);

        foreach (var p in new[]
                 {
                     nameof(ArInvoice.Amount), nameof(ArInvoice.VatAmount), nameof(ArInvoice.NetAmount),
                     nameof(ArInvoice.ReceivedAmount), nameof(ArInvoice.OutstandingAmount),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VatRate).HasColumnType("decimal(9,4)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.DocumentDate });
        builder.HasIndex(x => new { x.ClientCompanyId, x.CustomerCode });
    }
}
