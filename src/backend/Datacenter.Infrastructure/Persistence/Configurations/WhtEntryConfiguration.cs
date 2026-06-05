using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class WhtEntryConfiguration : IEntityTypeConfiguration<WhtEntry>
{
    public void Configure(EntityTypeBuilder<WhtEntry> builder)
    {
        builder.ToTable("WhtEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceKey).HasMaxLength(60).IsRequired();
        builder.Property(x => x.FormType).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.DocumentNo).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(30);
        builder.Property(x => x.PayeeName).HasMaxLength(120);
        builder.Property(x => x.PayeePrefix).HasMaxLength(20);
        builder.Property(x => x.PayeeTaxId).HasMaxLength(20);
        builder.Property(x => x.PayeeAddress).HasMaxLength(255);
        builder.Property(x => x.IncomeType).HasMaxLength(60);
        builder.Property(x => x.Condition).HasMaxLength(4);
        builder.Property(x => x.EmailStatus).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.EmailRecipient).HasMaxLength(200);
        builder.Property(x => x.EmailSentBy).HasMaxLength(100);
        builder.Property(x => x.EmailError).HasMaxLength(500);

        builder.Property(x => x.BaseAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxRate).HasColumnType("decimal(9,4)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // จัดกลุ่มรายงานตาม (บริษัท, เดือนภาษี, แบบ)
        builder.HasIndex(x => new { x.ClientCompanyId, x.TaxPeriod, x.FormType });

        // business key สำหรับ upsert ตอน re-import (Id เสถียร + คงสถานะส่งเมล)
        builder.HasIndex(x => new { x.ClientCompanyId, x.SourceKey }).IsUnique();
    }
}
