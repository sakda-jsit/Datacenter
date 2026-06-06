using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Datacenter.Infrastructure.Persistence.Configurations;

public class PayrollRateConfigConfiguration : IEntityTypeConfiguration<PayrollRateConfig>
{
    public void Configure(EntityTypeBuilder<PayrollRateConfig> builder)
    {
        builder.ToTable("PayrollRateConfigs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(300);
        foreach (var p in new[]
                 {
                     nameof(PayrollRateConfig.SsoEmployeePct), nameof(PayrollRateConfig.SsoEmployerPct),
                     nameof(PayrollRateConfig.WcfRatePct),
                 })
            builder.Property(p).HasColumnType("decimal(9,4)");
        foreach (var p in new[]
                 {
                     nameof(PayrollRateConfig.SsoWageFloor), nameof(PayrollRateConfig.SsoWageCap),
                     nameof(PayrollRateConfig.WcfWageCapPerYear),
                 })
            builder.Property(p).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.ClientCompanyId, x.EffectiveFrom });
    }
}
