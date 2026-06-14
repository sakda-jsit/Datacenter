using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class OfficeProfileConfiguration : IEntityTypeConfiguration<OfficeProfile>
{
    public void Configure(EntityTypeBuilder<OfficeProfile> builder)
    {
        builder.ToTable("OfficeProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OfficeName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(13);
        builder.Property(x => x.BranchCode).HasMaxLength(10);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Phone).HasMaxLength(50);
    }
}
