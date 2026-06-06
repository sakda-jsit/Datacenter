using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class PayrollAccountMappingConfiguration : IEntityTypeConfiguration<PayrollAccountMapping>
{
    public void Configure(EntityTypeBuilder<PayrollAccountMapping> builder)
    {
        builder.ToTable("PayrollAccountMappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Role).HasConversion<int>().HasDefaultValue(Datacenter.Domain.Enums.PayrollPostingRole.SalaryExpense);
        builder.Property(x => x.Department).HasMaxLength(100);
        builder.Property(x => x.Note).HasMaxLength(300);

        builder.HasOne<ClientCompany>()
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.AccountCode }).IsUnique();
    }
}
