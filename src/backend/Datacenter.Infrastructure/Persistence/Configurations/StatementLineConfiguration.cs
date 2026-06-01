using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class StatementLineConfiguration : IEntityTypeConfiguration<StatementLine>
{
    public void Configure(EntityTypeBuilder<StatementLine> builder)
    {
        builder.ToTable("StatementLines");
        builder.HasKey(x => x.RefCode);
        builder.Property(x => x.RefCode).HasMaxLength(10);
        builder.Property(x => x.LineName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Section).HasMaxLength(1);
    }
}
