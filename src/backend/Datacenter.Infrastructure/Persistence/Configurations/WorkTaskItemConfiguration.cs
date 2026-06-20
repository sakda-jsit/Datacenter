using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class WorkTaskItemConfiguration : IEntityTypeConfiguration<WorkTaskItem>
{
    public void Configure(EntityTypeBuilder<WorkTaskItem> builder)
    {
        builder.Property(i => i.Text).HasMaxLength(500).IsRequired();
        builder.HasIndex(i => i.WorkTaskId);
    }
}
