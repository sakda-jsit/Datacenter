using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankAccountCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Branch).HasMaxLength(40);
        builder.Property(x => x.ShortName).HasMaxLength(20);
        builder.Property(x => x.AccountNumber).HasMaxLength(20);
        builder.Property(x => x.GlAccountCode).HasMaxLength(20);
        builder.Property(x => x.BalanceForward).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.BankAccountCode }).IsUnique();
    }
}
