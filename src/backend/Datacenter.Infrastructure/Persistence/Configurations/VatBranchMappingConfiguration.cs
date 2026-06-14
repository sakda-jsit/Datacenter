using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class VatBranchMappingConfiguration : IEntityTypeConfiguration<VatBranchMapping>
{
    public void Configure(EntityTypeBuilder<VatBranchMapping> builder)
    {
        builder.ToTable("VatBranchMappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DepartmentCode).HasMaxLength(8);
        builder.Property(x => x.RdBranchNo).HasMaxLength(10).IsRequired();
        builder.Property(x => x.BranchName).HasMaxLength(120);

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.DepartmentCode }).IsUnique();
    }
}
