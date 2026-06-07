using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// เงินให้กู้ยืมที่คิดดอกเบี้ย (เช่น เงินให้กู้กรรมการ บัญชี 22120) — กระดาษทำการปิดงบ req v11 docs/13 INTEREST INCOME.
/// คำนวณดอกเบี้ยรับตามยอดเงินต้นคงเหลือ × อัตรา × จำนวนวัน / ฐานวันต่อปี (รองรับเงินต้นเปลี่ยนระหว่างปีผ่าน movement).
/// + ภาษีธุรกิจเฉพาะ (SBT) + รายได้ส่วนท้องถิ่น (% ของ SBT) เป็นยอดประกอบ.
/// generate รายการปรับปรุง: Dr ดอกเบี้ยค้างรับ / Cr รายได้ดอกเบี้ย. ป้อนมือ (Express ไม่มีทะเบียนนี้).
/// </summary>
public class InterestBearingLoan : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ชื่อ/ผู้กู้ (เช่น เงินให้กู้ยืมกรรมการ - นายสมชาย)</summary>
    public string Name { get; set; } = string.Empty;

    public string? Reference { get; set; }

    /// <summary>อัตราดอกเบี้ยต่อปี (%)</summary>
    public decimal AnnualRatePct { get; set; }

    /// <summary>อัตราภาษีธุรกิจเฉพาะ (%) — ปกติ 3.0</summary>
    public decimal SbtRatePct { get; set; } = 3.0m;

    /// <summary>รายได้ส่วนท้องถิ่น (% ของภาษีธุรกิจเฉพาะ) — ปกติ 10.0</summary>
    public decimal LocalTaxPctOfSbt { get; set; } = 10.0m;

    /// <summary>ฐานจำนวนวันต่อปี (ปกติ 365)</summary>
    public int DayCountBasis { get; set; } = 365;

    // ── ผูกบัญชี GL สำหรับ generate adjustment ────────────────────────────────
    /// <summary>บัญชีดอกเบี้ยค้างรับ (สินทรัพย์ ยอดธรรมชาติเดบิต)</summary>
    public int InterestReceivableAccountId { get; set; }

    /// <summary>บัญชีรายได้ดอกเบี้ย (รายได้)</summary>
    public int InterestIncomeAccountId { get; set; }

    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsActive { get; set; } = true;

    public ClientCompany ClientCompany { get; set; } = null!;
    public List<LoanPrincipalMovement> Movements { get; set; } = new();
}
