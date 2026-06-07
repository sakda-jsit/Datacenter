using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ใบตรวจนับเงินสด (กระดาษทำการปิดงบ — req v11 docs/13 CASH COUNT).
/// บันทึกชนิดธนบัตร/เหรียญ × จำนวนฉบับ = มูลค่า แล้วเทียบยอดนับจริงกับบัญชีเงินสดใน GL.
/// generate รายการปรับปรุง (เงินสดขาด/เกิน) เข้า TB ได้. ป้อนมือ (Express ไม่มีทะเบียนตรวจนับ).
/// </summary>
public class CashCount : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ปีบัญชี (ค.ศ.) ที่ตรวจนับ</summary>
    public int FiscalYear { get; set; }

    /// <summary>วันที่ตรวจนับ (ปกติ = วันสิ้นปีงบ)</summary>
    public DateTime CountDate { get; set; }

    /// <summary>คำอธิบาย/จุดเก็บเงินสด (เช่น เงินสดย่อย, เงินสดในมือ)</summary>
    public string? Reference { get; set; }

    /// <summary>บัญชีเงินสดใน GL ที่นำยอดนับจริงไปเทียบ (สินทรัพย์, ยอดธรรมชาติเดบิต)</summary>
    public int CashAccountId { get; set; }

    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsActive { get; set; } = true;

    public ClientCompany ClientCompany { get; set; } = null!;
    public List<CashCountLine> Lines { get; set; } = new();
}
