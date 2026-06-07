using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150);
        builder.Property(x => x.ModuleName).HasMaxLength(60);
        builder.Property(x => x.RecordRef).HasMaxLength(100);
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.VerifiedBy).HasMaxLength(100);
        builder.Property(x => x.Content).HasColumnType("varbinary(max)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear, x.Category });
        builder.HasIndex(x => new { x.ClientCompanyId, x.ModuleName, x.RecordId });
    }
}
