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
    DateTime? IssueDate);

/// <summary>สร้าง PDF หนังสือรับรองหัก ณ ที่จ่าย (QuestPDF + ฟอนต์ไทย) — 1 ใบ/หน้า</summary>
public interface IWhtCertificatePdfService
{
    byte[] Generate(IReadOnlyList<WhtCertificateModel> certificates);
}
