using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne<ClientCompany>()
            .WithMany().HasForeignKey(x => x.ClientCompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClientCompanyId, x.Year, x.Month }).IsUnique();

        builder.HasMany(x => x.Items)
            .WithOne(i => i.PayrollRun!)
            .HasForeignKey(i => i.PayrollRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> builder)
    {
        builder.ToTable("PayrollItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);

        foreach (var p in new[]
                 {
                     nameof(PayrollItem.Salary), nameof(PayrollItem.DailyWageDays), nameof(PayrollItem.DailyWageRate),
                     nameof(PayrollItem.HousingAllowance), nameof(PayrollItem.FoodAllowance), nameof(PayrollItem.Overtime),
                     nameof(PayrollItem.Diligence), nameof(PayrollItem.Bonus), nameof(PayrollItem.OtherIncome),
                     nameof(PayrollItem.GrossIncome), nameof(PayrollItem.SsoWageBase), nameof(PayrollItem.SsoEmployee),
                     nameof(PayrollItem.WithholdingTax), nameof(PayrollItem.Absence), nameof(PayrollItem.OtherDeduction),
                     nameof(PayrollItem.NetPay),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Employee)
            .WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId }).IsUnique();
    }
}
