using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// เอกสารแนบ/หลักฐาน (Attachment / Evidence) — docs/18 §17-20.
/// แนบเข้ากับระเบียนของโมดูลใดก็ได้ (polymorphic ผ่าน ModuleName + RecordId) หรือผูกแค่ระดับบริษัท+ปีบัญชี.
/// เก็บไฟล์เป็น blob ใน DB เหมือน EmployeeDocument + checksum (SHA-256) เพื่อพิสูจน์ความครบถ้วน.
/// เก็บอย่างน้อย 10 ปี (คำตอบ #13). ทุกการอัปโหลด/ดู/ลบ/ตรวจ ลง audit trail.
/// </summary>
public class Attachment : BaseEntity
{
    public int ClientCompanyId { get; set; }

    public AttachmentCategory Category { get; set; } = AttachmentCategory.Other;

    /// <summary>ปีบัญชี (AD) ที่เอกสารนี้เป็นหลักฐาน — ใช้ตรวจความครบถ้วนต่อปี (null = ไม่ผูกปี)</summary>
    public int? FiscalYear { get; set; }

    /// <summary>โมดูล/entity ต้นทางที่เอกสารนี้แนบ เช่น "AdjustmentEntry", "Bank", "Vat" (null = ระดับบริษัท)</summary>
    public string? ModuleName { get; set; }

    /// <summary>id ของระเบียนต้นทางที่แนบ (ถ้ามี)</summary>
    public int? RecordId { get; set; }

    /// <summary>ป้ายอ้างอิงอ่านง่าย เช่น เลขที่เอกสาร/เลขสัญญา</summary>
    public string? RecordRef { get; set; }

    /// <summary>หัวข้อ/คำอธิบายเอกสาร</summary>
    public string Title { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = [];
    public long ByteSize { get; set; }

    /// <summary>SHA-256 ของเนื้อไฟล์ (hex) — พิสูจน์ความครบถ้วนของหลักฐาน</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>วันที่ของเอกสาร (เช่น วันที่ใน statement/ใบกำกับ)</summary>
    public DateTime? DocumentDate { get; set; }

    public AttachmentVerificationStatus VerificationStatus { get; set; } = AttachmentVerificationStatus.Pending;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public string? Note { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
