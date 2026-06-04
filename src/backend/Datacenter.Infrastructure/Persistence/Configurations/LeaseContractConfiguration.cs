using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class LeaseContractConfiguration : IEntityTypeConfiguration<LeaseContract>
{
    public void Configure(EntityTypeBuilder<LeaseContract> builder)
    {
        builder.ToTable("LeaseContracts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ContractType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ContractNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AssetName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AssetCode).HasMaxLength(50);
        builder.Property(x => x.Lessor).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);

        // เงินจำนวน — decimal(18,2)
        foreach (var p in new[]
                 {
                     nameof(LeaseContract.CashPrice), nameof(LeaseContract.DownPayment),
                     nameof(LeaseContract.FinancedPrincipal), nameof(LeaseContract.InstallmentAmount),
                     nameof(LeaseContract.VatPerPeriod),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.IsActive });
        builder.HasIndex(x => new { x.ClientCompanyId, x.ContractNo }).IsUnique();
    }
}
