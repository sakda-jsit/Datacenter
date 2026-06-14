using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class Cit50ScheduleLineConfiguration : IEntityTypeConfiguration<Cit50ScheduleLine>
{
    public void Configure(EntityTypeBuilder<Cit50ScheduleLine> b)
    {
        b.ToTable("Cit50ScheduleLines");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Label).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class AccountCit50MappingConfiguration : IEntityTypeConfiguration<AccountCit50Mapping>
{
    public void Configure(EntityTypeBuilder<AccountCit50Mapping> b)
    {
        b.ToTable("AccountCit50Mappings");
        b.HasKey(x => x.Id);
        b.Property(x => x.AccountCode).HasMaxLength(30).IsRequired();
        b.Property(x => x.AccountName).HasMaxLength(200);
        b.Property(x => x.Cit50LineCode).HasMaxLength(30).IsRequired();
        b.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClientCompanyId, x.AccountCode }).IsUnique();
    }
}
