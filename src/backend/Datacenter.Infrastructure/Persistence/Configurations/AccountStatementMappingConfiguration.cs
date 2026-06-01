using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class AccountStatementMappingConfiguration : IEntityTypeConfiguration<AccountStatementMapping>
{
    public void Configure(EntityTypeBuilder<AccountStatementMapping> builder)
    {
        builder.ToTable("AccountStatementMappings");
        // Composite PK: one account maps to one REF code per company
        builder.HasKey(x => new { x.ClientCompanyId, x.AccountCode });
        builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccountName).HasMaxLength(200);
        builder.Property(x => x.RefCode).HasMaxLength(10).IsRequired();

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StatementLine)
            .WithMany()
            .HasForeignKey(x => x.RefCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.RefCode });
    }
}
