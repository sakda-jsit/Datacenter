using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class SsoEnrollmentConfiguration : IEntityTypeConfiguration<SsoEnrollment>
{
    public void Configure(EntityTypeBuilder<SsoEnrollment> builder)
    {
        builder.ToTable("SsoEnrollments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => x.EmployeeId);
    }
}
