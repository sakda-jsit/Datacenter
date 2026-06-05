namespace Datacenter.Application.Common.Interfaces;

/// <summary>ข้อมูลสำหรับพิมพ์หนังสือรับรองการหักภาษี ณ ที่จ่าย (50 ทวิ) หนึ่งใบ</summary>
public record WhtCertificateModel(
    string FormLabel,          // "ภ.ง.ด.3" | "ภ.ง.ด.53"
    string SequenceNo,         // เลขที่/ลำดับในแบบยื่น (DocumentNo)
    // ผู้มีหน้าที่หักภาษี ณ ที่จ่าย (บริษัทลูกค้า)
    string PayerName,
    string PayerTaxId,
    string? PayerAddress,
    // ผู้ถูกหักภาษี ณ ที่จ่าย
    string PayeeName,
    string PayeeTaxId,
    string? PayeeAddress,
    // รายการเงินได้
    string IncomeType,
    DateTime? PayDate,
    decimal Amount,
    decimal TaxAmount,
    decimal TaxRate,
    string AmountInWords,      // รวมภาษีเป็นตัวอักษร
    DateTime? IssueDate,
    // หมวดเงินได้ที่จะลงจำนวนเงิน (1..6 ตามแบบ 50 ทวิ; 41=40(4)(ก) ดอกเบี้ย, 42=40(4)(ข) เงินปันผล)
    int IncomeCategory,
    // เงื่อนไขการหัก/ออกภาษี (1=หักภาษี ณ ที่จ่าย, 2=ออกให้ตลอดไป, 3=ออกภาษีให้ครั้งเดียว, 4=อื่นๆ)
    int ConditionType,
    // รหัสสาขาผู้หัก ("00000"/ว่าง = สำนักงานใหญ่) — แสดงเป็นข้อความในกล่องผู้มีหน้าที่หัก
    string? PayerBranchCode = null,
    // รูปลายเซ็นผู้มีหน้าที่หัก ณ ที่จ่าย (PNG/JPG) — ถ้ามีจะวางเหนือเส้นลงชื่อ
    byte[]? PayerSignature = null);

/// <summary>สร้าง PDF หนังสือรับรองหัก ณ ที่จ่าย (QuestPDF + ฟอนต์ไทย) — 1 ใบ/หน้า</summary>
public interface IWhtCertificatePdfService
{
    byte[] Generate(IReadOnlyList<WhtCertificateModel> certificates);

    /// <summary>เรนเดอร์เป็นรูป PNG หน้าละ 1 รูป (สำหรับ preview ในเว็บ — เลี่ยงปัญหา iframe PDF ขึ้นจอดำ)</summary>
    IReadOnlyList<byte[]> GenerateImages(IReadOnlyList<WhtCertificateModel> certificates);
}
