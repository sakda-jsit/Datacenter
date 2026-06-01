using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class FsExternalInputConfiguration : IEntityTypeConfiguration<FsExternalInput>
{
    public void Configure(EntityTypeBuilder<FsExternalInput> builder)
    {
        builder.ToTable("FsExternalInputs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RefCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // One external input per (company, year, refcode)
        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear, x.RefCode }).IsUnique();
    }
}
