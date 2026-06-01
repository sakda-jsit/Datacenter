using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class ComplianceTaskConfiguration : IEntityTypeConfiguration<ComplianceTask>
{
    public void Configure(EntityTypeBuilder<ComplianceTask> builder)
    {
        builder.HasIndex(t => new { t.ClientCompanyId, t.Year, t.Month, t.TaskType })
               .IsUnique();

        builder.HasIndex(t => new { t.ClientCompanyId, t.DueDate });
        builder.HasIndex(t => t.Status);

        builder.Property(t => t.TaskType).HasConversion<int>();
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.Note).HasMaxLength(500);

        builder.HasOne(t => t.ClientCompany)
               .WithMany()
               .HasForeignKey(t => t.ClientCompanyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.AssignedUser)
               .WithMany()
               .HasForeignKey(t => t.AssignedUserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.CompletedByUser)
               .WithMany()
               .HasForeignKey(t => t.CompletedByUserId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
