using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class CompanyAuditorConfiguration : IEntityTypeConfiguration<CompanyAuditor>
{
    public void Configure(EntityTypeBuilder<CompanyAuditor> builder)
    {
        builder.ToTable("CompanyAuditors");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Auditor)
            .WithMany().HasForeignKey(x => x.AuditorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bookkeeper)
            .WithMany().HasForeignKey(x => x.BookkeeperId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear }).IsUnique();

        // legacy free-text (deprecated — backfill แล้วค่อยลบ)
        builder.Property(x => x.AuditorName).HasMaxLength(200);
        builder.Property(x => x.AuditorLicenseNo).HasMaxLength(20);
        builder.Property(x => x.AuditorTaxId).HasMaxLength(13);
        builder.Property(x => x.BookkeeperName).HasMaxLength(200);
        builder.Property(x => x.BookkeeperTaxId).HasMaxLength(13);
        builder.Property(x => x.AuditFirmName).HasMaxLength(200);
        builder.Property(x => x.AuditFirmTaxId).HasMaxLength(13);
    }
}

public class AuditorConfiguration : IEntityTypeConfiguration<Auditor>
{
    public void Configure(EntityTypeBuilder<Auditor> builder)
    {
        builder.ToTable("Auditors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.LicenseNo).HasMaxLength(20);
        builder.Property(x => x.TaxId).HasMaxLength(13);
        builder.Property(x => x.AuditFirmName).HasMaxLength(200);
        builder.Property(x => x.AuditFirmTaxId).HasMaxLength(13);
    }
}

public class BookkeeperConfiguration : IEntityTypeConfiguration<Bookkeeper>
{
    public void Configure(EntityTypeBuilder<Bookkeeper> builder)
    {
        builder.ToTable("Bookkeepers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(13);
    }
}
