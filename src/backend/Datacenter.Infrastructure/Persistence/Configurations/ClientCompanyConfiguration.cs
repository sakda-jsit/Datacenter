using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ClientCompanyConfiguration : IEntityTypeConfiguration<ClientCompany>
{
    public void Configure(EntityTypeBuilder<ClientCompany> builder)
    {
        builder.ToTable("ClientCompanies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EnglishName).HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(13);
        builder.Property(x => x.BranchCode).HasMaxLength(5).HasDefaultValue("00000");
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.HasIndex(x => x.Code).IsUnique();

        // business key สำหรับ match ตอน import (filtered: เฉพาะ active ที่มี TaxId — เว้น onboard ที่ยังไม่กรอก/สำเนาที่ปิดใช้งาน)
        builder.HasIndex(x => new { x.TaxId, x.BranchCode })
            .IsUnique()
            .HasFilter("[TaxId] <> '' AND [IsActive] = 1");
    }
}
