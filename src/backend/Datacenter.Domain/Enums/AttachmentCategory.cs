namespace Datacenter.Domain.Enums;

/// <summary>
/// ประเภทเอกสารแนบ/หลักฐานปิดงบ (docs/18 §17). serialize เป็น int.
/// ใช้จัดกลุ่ม + ตรวจความครบถ้วน (evidence completeness) ก่อนปิดงบ/สร้างชุดรายงาน.
/// </summary>
public enum AttachmentCategory
{
    Other = 0,
    BankStatement = 1,          // bank statement / รายการเดินบัญชี
    TaxInvoice = 2,             // ใบกำกับภาษี
    WhtCertificate = 3,         // หนังสือรับรองหัก ณ ที่จ่าย (50 ทวิ)
    RevenueFiling = 4,          // แบบ/ใบเสร็จสรรพากร (ภ.พ.30, ภ.ง.ด., PDF e-Filing)
    SocialSecurityFiling = 5,   // เอกสารประกันสังคม (สปส.1-10, ใบเสร็จ)
    FixedAssetDocument = 6,     // เอกสารสินทรัพย์ (ใบกำกับ/ทะเบียน)
    PrepaidDocument = 7,        // เอกสารค่าใช้จ่ายจ่ายล่วงหน้า
    ContractDocument = 8,       // สัญญาเช่าซื้อ/เงินกู้
    FinancialStatement = 9,     // งบการเงิน (ฉบับลงนาม)
    ImportFile = 10,            // ไฟล์ Express/นำเข้า
    BankConfirmation = 11,      // หนังสือยืนยันยอดธนาคาร
}
