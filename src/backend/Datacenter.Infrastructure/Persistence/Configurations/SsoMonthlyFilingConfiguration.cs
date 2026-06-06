using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class SsoMonthlyFilingConfiguration : IEntityTypeConfiguration<SsoMonthlyFiling>
{
    public void Configure(EntityTypeBuilder<SsoMonthlyFiling> builder)
    {
        builder.ToTable("SsoMonthlyFilings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ReceiptNo).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.FormFileName).HasMaxLength(260);
        builder.Property(x => x.FormContentType).HasMaxLength(120);
        builder.Property(x => x.ReceiptFileName).HasMaxLength(260);
        builder.Property(x => x.ReceiptContentType).HasMaxLength(120);

        foreach (var p in new[]
                 {
                     nameof(SsoMonthlyFiling.SnapshotTotalWage), nameof(SsoMonthlyFiling.SnapshotEmployeeContribution),
                     nameof(SsoMonthlyFiling.SnapshotEmployerContribution), nameof(SsoMonthlyFiling.SnapshotGrandTotal),
                     nameof(SsoMonthlyFiling.ReceiptAmount),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.PayrollRun)
            .WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Cascade);

        // 1 งวด = 1 การยื่น
        builder.HasIndex(x => x.PayrollRunId).IsUnique();
        builder.HasIndex(x => new { x.ClientCompanyId, x.Year, x.Month });
    }
}
