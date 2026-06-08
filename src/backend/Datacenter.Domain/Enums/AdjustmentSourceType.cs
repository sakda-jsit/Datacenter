namespace Datacenter.Domain.Enums;

/// <summary>
/// ที่มาของรายการปรับปรุง (adjustment entry) ในกระดาษทำการปิดงบ.
/// โมดูล Leasing/Loan/FixedAsset/Prepaid/Stock จะ generate adjustment พร้อมระบุ SourceType
/// เพื่อตามรอยกลับไปยังกระดาษทำการต้นทาง.
/// </summary>
public enum AdjustmentSourceType
{
    /// <summary>ปรับปรุงด้วยมือโดยเจ้าหน้าที่บัญชี</summary>
    Manual = 0,
    /// <summary>มาจากกระดาษทำการสัญญาเช่า (leasing schedule)</summary>
    Leasing = 1,
    /// <summary>มาจากกระดาษทำการเงินกู้ (loan schedule)</summary>
    Loan = 2,
    /// <summary>ปรับปรุงทางภาษี</summary>
    Tax = 3,
    /// <summary>อื่น ๆ</summary>
    Other = 4,
    /// <summary>มาจากกระดาษทำการสินทรัพย์ถาวร (ค่าเสื่อมราคา/จำหน่าย)</summary>
    FixedAsset = 5,
    /// <summary>มาจากกระดาษทำการค่าใช้จ่ายจ่ายล่วงหน้า (ตัดจ่ายตามงวด)</summary>
    Prepaid = 6,
    /// <summary>มาจากกระดาษทำการตรวจนับเงินสด (เงินสดขาด/เกิน)</summary>
    CashCount = 7,
    /// <summary>มาจากกระดาษทำการดอกเบี้ยรับเงินให้กู้ (รับรู้ดอกเบี้ยค้างรับ)</summary>
    InterestIncome = 8,
    /// <summary>มาจากการกระทบยอดธนาคาร (รายการในธนาคารที่สมุดยังไม่ลง เช่น ค่าธรรมเนียม/ดอกเบี้ย)</summary>
    BankReconciliation = 9
}
