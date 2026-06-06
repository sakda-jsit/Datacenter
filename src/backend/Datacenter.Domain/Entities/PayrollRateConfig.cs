using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// อัตราเงินสมทบประกันสังคม + กองทุนเงินทดแทน แบบ <b>effective-dated</b> (มีผลตามวันที่).
/// ปรับอัตราใหม่ = เพิ่มแถวใหม่ (EffectiveFrom ใหม่) ไม่ทับของเก่า → งวดที่คำนวณไปแล้วไม่เปลี่ยน.
/// ClientCompanyId = null → ค่ากลาง (default ทุกบริษัท); มีค่า → override เฉพาะบริษัทนั้น.
/// </summary>
public class PayrollRateConfig : BaseEntity
{
    public int? ClientCompanyId { get; set; }          // null = ค่ากลาง
    public DateTime EffectiveFrom { get; set; }         // มีผลตั้งแต่วันที่

    // ประกันสังคม
    public decimal SsoEmployeePct { get; set; } = 5m;   // ผู้ประกันตนหัก %
    public decimal SsoEmployerPct { get; set; } = 5m;   // นายจ้างสมทบ %
    public decimal SsoWageFloor { get; set; } = 1650m;  // ฐานค่าจ้างขั้นต่ำ
    public decimal SsoWageCap { get; set; } = 15000m;   // ฐานค่าจ้างสูงสุด

    // กองทุนเงินทดแทน (กท.20)
    public decimal WcfRatePct { get; set; } = 0.2m;     // อัตราเงินสมทบ %
    public decimal WcfWageCapPerYear { get; set; } = 240000m; // เพดานค่าจ้างต่อคนต่อปี

    public string? Note { get; set; }
}
