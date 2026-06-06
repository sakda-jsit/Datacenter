using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Payroll.Services;

/// <summary>ตัวช่วยเลือกอัตราค่ากลางที่มีผล ณ วันที่กำหนด (effective-dated; เปลี่ยนรายเดือนได้)</summary>
public static class PayrollRates
{
    /// <summary>เลือกอัตราที่มีผล: EffectiveFrom &lt;= asOf ล่าสุด (อัตราเป็นค่ากลางทั้งหมด)</summary>
    public static PayrollRateConfig? ResolveEffective(IEnumerable<PayrollRateConfig> all, DateTime asOf)
        => all
            .Where(c => c.EffectiveFrom.Date <= asOf.Date)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();
}
