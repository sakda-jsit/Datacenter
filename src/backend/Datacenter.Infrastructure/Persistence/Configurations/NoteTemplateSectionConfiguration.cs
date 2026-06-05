using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class NoteTemplateSectionConfiguration : IEntityTypeConfiguration<NoteTemplateSection>
{
    public void Configure(EntityTypeBuilder<NoteTemplateSection> builder)
    {
        builder.ToTable("NoteTemplateSections");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NoteKey).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.BodyText).HasColumnType("nvarchar(max)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // หนึ่ง section ต่อ (บริษัท/กลาง, ปีมีผล, รหัสหมายเหตุ)
        builder.HasIndex(x => new { x.ClientCompanyId, x.EffectiveYear, x.NoteKey }).IsUnique();
    }
}
