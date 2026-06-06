using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ToTable("EmployeeDocuments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100);
        builder.Property(x => x.Content).HasColumnType("varbinary(max)");
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.DocType });
    }
}
