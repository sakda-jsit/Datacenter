using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.SourceModule).HasMaxLength(50);

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Primary query pattern: company + fiscal year (รายงานต่อปีงบ)
        builder.HasIndex(x => new { x.ClientCompanyId, x.FiscalYear });
        // คงไว้: partial-month date slice + idempotent delete by DocumentNo
        builder.HasIndex(x => new { x.ClientCompanyId, x.JournalDate });
        builder.HasIndex(x => new { x.ClientCompanyId, x.DocumentNo });
    }
}
