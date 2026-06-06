using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ค่าใช้จ่ายจ่ายล่วงหน้า (กระดาษทำการปิดงบ — req v11 docs/14 PREPAID).
/// ตัดจ่ายตามวันเริ่ม–สิ้นสุดแบบเส้นตรง (เดือนแรก/ปีแรก prorate ตามวัน) — คำนวณสด ไม่เก็บ schedule.
/// generate รายการปรับปรุง (AdjustmentEntry): Dr ค่าใช้จ่าย / Cr ค่าใช้จ่ายจ่ายล่วงหน้า (ตัดจ่ายในปีงบ).
/// ป้อนมือ (Express ไม่มีทะเบียน prepaid).
/// </summary>
public class PrepaidExpense : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัส (ถ้ามี)</summary>
    public string? Code { get; set; }

    /// <summary>รายละเอียด (เช่น ค่าเบี้ยประกัน, ค่า Antivirus รายปี)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>เอกสารอ้างอิง (เลขที่ใบกำกับ/สัญญา)</summary>
    public string? Reference { get; set; }

    /// <summary>มูลค่าตั้งต้น (ยอดที่จ่ายล่วงหน้า)</summary>
    public decimal TotalAmount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // ── ผูกบัญชี GL สำหรับ generate adjustment ────────────────────────────────
    /// <summary>บัญชีค่าใช้จ่ายจ่ายล่วงหน้า (สินทรัพย์, ยอดธรรมชาติเดบิต) — contra ของการตัดจ่าย</summary>
    public int PrepaidAccountId { get; set; }

    /// <summary>บัญชีค่าใช้จ่าย (P&amp;L) — ปลายทางการรับรู้ค่าใช้จ่ายในปี</summary>
    public int ExpenseAccountId { get; set; }

    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsActive { get; set; } = true;

    public ClientCompany ClientCompany { get; set; } = null!;
}
