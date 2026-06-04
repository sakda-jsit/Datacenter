namespace Datacenter.Domain.Enums;

/// <summary>
/// ประเภทสัญญาในกระดาษทำการเช่าซื้อ/เงินกู้.
/// - HirePurchase: เช่าซื้อ/ลีสซิ่ง — หนี้สิน gross (รวมดอกเบี้ยรอตัด + ภาษีซื้อยังไม่ถึงกำหนด), ตัดบัญชีแบบ effective interest
/// - Loan: เงินกู้ — หนี้สิน = เงินต้นคงเหลือ, ดอกเบี้ยเป็นค่าใช้จ่ายเมื่อเกิด (ไม่มี deferred/VAT)
/// </summary>
public enum LeaseContractType
{
    HirePurchase = 0,
    Loan = 1,
}
