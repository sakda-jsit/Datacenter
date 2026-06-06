using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายการเงินเดือนต่อพนักงานต่องวด — ตัวเลขจริงจาก slip (กรอกมือ);
/// ระบบคำนวณ ปกส./ภาษีเป็นตัวเทียบ (cross-check) ตอน query.
/// </summary>
public class PayrollItem : BaseEntity
{
    public int PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    // ── รายได้ (กรอกจาก slip) ──
    public decimal Salary { get; set; }            // เงินเดือน
    public decimal DailyWageDays { get; set; }     // วันทำงาน (กรณีรายวัน)
    public decimal DailyWageRate { get; set; }     // ค่าจ้างวันละ
    public decimal HousingAllowance { get; set; }  // ค่าที่พักอาศัย
    public decimal FoodAllowance { get; set; }     // ค่าอาหาร
    public decimal Overtime { get; set; }          // ค่าล่วงเวลา
    public decimal Diligence { get; set; }         // เบี้ยขยัน
    public decimal Bonus { get; set; }             // โบนัส
    public decimal OtherIncome { get; set; }       // รายได้อื่น
    public decimal GrossIncome { get; set; }       // รวมรายได้ (คำนวณ)

    // ── ประกันสังคม ──
    public decimal SsoWageBase { get; set; }       // รายได้ยื่น ปกส. (ฐานหลังเพดาน)
    public decimal SsoEmployee { get; set; }       // หัก ปกส. จริงจาก slip

    // ── ภาษี + หักอื่น ──
    public decimal WithholdingTax { get; set; }    // ภาษีหัก ณ ที่จ่าย (TAX)
    public decimal Absence { get; set; }           // ขาดงาน/มาสาย
    public decimal OtherDeduction { get; set; }    // หักอื่นๆ
    public decimal NetPay { get; set; }            // เงินสุทธิ (คำนวณ)

    public string? Note { get; set; }
}
