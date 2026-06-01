using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class CompanyUserAccessConfiguration : IEntityTypeConfiguration<CompanyUserAccess>
{
    public void Configure(EntityTypeBuilder<CompanyUserAccess> builder)
    {
        builder.ToTable("CompanyUserAccesses");
        builder.HasKey(x => new { x.UserId, x.ClientCompanyId });
        builder.Property(x => x.RoleInCompany).HasConversion<int>();

        builder.HasOne(x => x.User).WithMany(u => u.CompanyAccesses).HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.ClientCompany).WithMany(c => c.UserAccesses).HasForeignKey(x => x.ClientCompanyId);
    }
}
