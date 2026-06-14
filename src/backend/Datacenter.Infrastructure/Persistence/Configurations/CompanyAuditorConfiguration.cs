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

        builder.Property(x => x.AuditorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AuditorLicenseNo).HasMaxLength(20);
        builder.Property(x => x.AuditorTaxId).HasMaxLength(13);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear }).IsUnique();
    }
}
