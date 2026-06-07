using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class InterestBearingLoanConfiguration : IEntityTypeConfiguration<InterestBearingLoan>
{
    public void Configure(EntityTypeBuilder<InterestBearingLoan> builder)
    {
        builder.ToTable("InterestBearingLoans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.AttachmentPath).HasMaxLength(500);
        builder.Property(x => x.AnnualRatePct).HasColumnType("decimal(9,4)");
        builder.Property(x => x.SbtRatePct).HasColumnType("decimal(9,4)");
        builder.Property(x => x.LocalTaxPctOfSbt).HasColumnType("decimal(9,4)");

        builder.HasOne(x => x.ClientCompany)
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Movements)
            .WithOne(m => m.Loan)
            .HasForeignKey(m => m.InterestBearingLoanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ClientCompanyId, x.IsActive });
    }
}

public class LoanPrincipalMovementConfiguration : IEntityTypeConfiguration<LoanPrincipalMovement>
{
    public void Configure(EntityTypeBuilder<LoanPrincipalMovement> builder)
    {
        builder.ToTable("LoanPrincipalMovements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Description).HasMaxLength(200);
    }
}
