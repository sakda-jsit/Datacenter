using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankAccountCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.TransactionType).HasMaxLength(4);
        builder.Property(x => x.ChequeNo).HasMaxLength(20);
        builder.Property(x => x.CounterpartyName).HasMaxLength(120);
        builder.Property(x => x.Remark).HasMaxLength(120);
        builder.Property(x => x.Voucher).HasMaxLength(20);
        builder.Property(x => x.ChequeStatus).HasMaxLength(4);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Charge).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany()
            .HasForeignKey(x => x.ClientCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.BankAccountCode, x.TransactionDate });
    }
}
