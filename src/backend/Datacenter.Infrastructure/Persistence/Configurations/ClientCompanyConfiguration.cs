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
        builder.Property(x => x.SsoAccountNo).HasMaxLength(15);
        builder.Property(x => x.SsoBranchCode).HasMaxLength(6);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.HasIndex(x => x.Code).IsUnique();

        // ผู้ลงนามประจำบริษัท (master, default ทุกปี)
        builder.HasOne(x => x.DefaultAuditor)
            .WithMany().HasForeignKey(x => x.DefaultAuditorId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.DefaultBookkeeper)
            .WithMany().HasForeignKey(x => x.DefaultBookkeeperId).OnDelete(DeleteBehavior.SetNull);

        // business key สำหรับ match ตอน import (filtered: เฉพาะ active ที่มี TaxId — เว้น onboard ที่ยังไม่กรอก/สำเนาที่ปิดใช้งาน)
        builder.HasIndex(x => new { x.TaxId, x.BranchCode })
            .IsUnique()
            .HasFilter("[TaxId] <> '' AND [IsActive] = 1");
    }
}
