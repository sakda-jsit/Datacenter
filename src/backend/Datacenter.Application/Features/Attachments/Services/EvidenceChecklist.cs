using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Attachments.Services;

/// <summary>หนึ่งหมวดใน checklist หลักฐานปิดงบ.</summary>
public record EvidenceChecklistItem(AttachmentCategory Category, string Label, bool Required);

/// <summary>
/// รายการหลักฐานมาตรฐานที่ควรแนบก่อนปิดงบ/สร้างชุดรายงาน (docs/18 §17 — completeness check).
/// หมวดที่ Required=true หากยังไม่มีเอกสาร → ถือว่าหลักฐานไม่ครบ (warning).
/// ลำดับในรายการ = ลำดับที่แสดงใน checklist.
/// </summary>
public static class EvidenceChecklist
{
    public static readonly IReadOnlyList<EvidenceChecklistItem> Items =
    [
        new(AttachmentCategory.FinancialStatement, "งบการเงิน (ฉบับลงนาม)", true),
        new(AttachmentCategory.BankConfirmation, "หนังสือยืนยันยอดธนาคาร", true),
        new(AttachmentCategory.BankStatement, "Bank statement", true),
        new(AttachmentCategory.RevenueFiling, "แบบ/ใบเสร็จสรรพากร", true),
        new(AttachmentCategory.WhtCertificate, "หนังสือรับรองหัก ณ ที่จ่าย", false),
        new(AttachmentCategory.TaxInvoice, "ใบกำกับภาษี", false),
        new(AttachmentCategory.SocialSecurityFiling, "เอกสารประกันสังคม", false),
        new(AttachmentCategory.FixedAssetDocument, "เอกสารสินทรัพย์ถาวร", false),
        new(AttachmentCategory.PrepaidDocument, "เอกสารค่าใช้จ่ายจ่ายล่วงหน้า", false),
        new(AttachmentCategory.ContractDocument, "สัญญาเช่าซื้อ/เงินกู้", false),
        new(AttachmentCategory.ImportFile, "ไฟล์นำเข้า Express", false),
    ];
}
