namespace Datacenter.Domain.Enums;

/// <summary>
/// ชุดค่าเสื่อมราคา — ต้องเก็บแยกชุดบัญชีและชุดภาษี (req v11 docs/14: "ห้ามรวมยอดโดยไม่มีประเภทกำกับ").
/// - Book: ตามมาตรฐานบัญชี (ใช้ลงงบการเงิน/GL)
/// - Tax: ตามประมวลรัษฎากร (ใช้ปรับปรุงทางภาษี)
/// </summary>
public enum DepreciationSet
{
    Book = 0,
    Tax = 1,
}
