using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.HasIndex(t => new { t.ClientCompanyId, t.Status });
        builder.HasIndex(t => new { t.ClientCompanyId, t.DueDate });
        builder.HasIndex(t => t.AssignedUserId);

        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.Category).HasMaxLength(100);
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.Priority).HasConversion<int>();

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
