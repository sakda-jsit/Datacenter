using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// แมพ "รหัสบัญชีเงินเดือน" ใน Express (GLACC เช่น 5310-01 เงินเดือน[บริหาร], 5150-01 ฝ่ายผลิต)
/// → ฝ่าย/แผนก ต่อบริษัท. ใช้ตอน import พนักงานจาก Express: scan GLJNLIT บัญชีเหล่านี้ →
/// voucher → APTRN → SUPCOD → APMAS = พนักงาน (Express เก็บพนักงานเป็นเจ้าหนี้).
/// </summary>
public class PayrollAccountMapping : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสบัญชี GL เงินเดือน (เช่น "5310-01")</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>ฝ่าย/แผนก (เช่น "ฝ่ายบริหาร", "ฝ่ายผลิต")</summary>
    public string Department { get; set; } = string.Empty;

    public string? Note { get; set; }
}
