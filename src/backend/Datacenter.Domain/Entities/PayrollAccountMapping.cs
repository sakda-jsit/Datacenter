using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// แมพรหัสบัญชี GL → บทบาทในใบสำคัญลงบัญชีเงินเดือน (+ ฝ่าย) ต่อบริษัท.
/// ใช้ 2 อย่าง: (1) import พนักงานจาก Express (เฉพาะ role เงินเดือน/ค่าจ้าง — scan GLJNLIT
/// บัญชีเหล่านี้ → voucher → APTRN → SUPCOD → APMAS = พนักงาน) (2) generate ใบสำคัญ + กระทบยอด GL.
/// </summary>
public class PayrollAccountMapping : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสบัญชี GL (เช่น "5310-01")</summary>
    public string AccountCode { get; set; } = string.Empty;

    /// <summary>บทบาทบัญชี (เงินเดือน/ค่าจ้าง/ปกส.รอนำส่ง/...). เดิม=เงินเดือน</summary>
    public PayrollPostingRole Role { get; set; } = PayrollPostingRole.SalaryExpense;

    /// <summary>ฝ่าย/แผนก (เช่น "ฝ่ายบริหาร", "ฝ่ายผลิต"); ว่าง = ทั้งบริษัท (เช่น ปกส.รอนำส่ง)</summary>
    public string? Department { get; set; }

    public string? Note { get; set; }
}
