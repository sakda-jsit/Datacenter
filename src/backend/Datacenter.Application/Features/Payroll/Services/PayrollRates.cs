using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Payroll.Services;

/// <summary>ตัวช่วยเลือกอัตราที่มีผล ณ วันที่กำหนด (effective-dated; บริษัทเฉพาะ override ค่ากลาง)</summary>
public static class PayrollRates
{
    /// <summary>
    /// เลือก config ที่มีผล: เอาเฉพาะ EffectiveFrom &lt;= asOf และเป็นค่ากลางหรือของบริษัทนั้น
    /// แล้วให้ของบริษัทมาก่อนค่ากลาง จากนั้นเอา EffectiveFrom ล่าสุด.
    /// </summary>
    public static PayrollRateConfig? ResolveEffective(
        IEnumerable<PayrollRateConfig> all, int companyId, DateTime asOf)
        => all
            .Where(c => c.EffectiveFrom.Date <= asOf.Date
                        && (c.ClientCompanyId == null || c.ClientCompanyId == companyId))
            .OrderByDescending(c => c.ClientCompanyId == companyId)   // บริษัทเฉพาะมาก่อน
            .ThenByDescending(c => c.EffectiveFrom)
            .FirstOrDefault();
}
