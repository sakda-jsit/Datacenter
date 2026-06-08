using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ComplianceTaskTemplateConfiguration : IEntityTypeConfiguration<ComplianceTaskTemplate>
{
    public void Configure(EntityTypeBuilder<ComplianceTaskTemplate> builder)
    {
        builder.ToTable("ComplianceTaskTemplates");
        builder.HasKey(x => x.Id);

        // enum เก็บเป็น int (ค่าเริ่มต้น)

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // ระดับเฉพาะบริษัท: หนึ่งแถวต่อ (บริษัท, ประเภทงาน)
        builder.HasIndex(x => new { x.ClientCompanyId, x.TaskType })
            .IsUnique()
            .HasFilter("[ClientCompanyId] IS NOT NULL");

        // ระดับ global: หนึ่งแถวต่อประเภทงาน
        builder.HasIndex(x => x.TaskType)
            .IsUnique()
            .HasFilter("[ClientCompanyId] IS NULL");
    }
}
