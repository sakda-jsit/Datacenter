namespace Datacenter.Domain.Enums;

/// <summary>สถานะตรวจสอบเอกสารแนบ (docs/18 §17 — metadata: สถานะตรวจสอบ). serialize เป็น int.</summary>
public enum AttachmentVerificationStatus
{
    Pending = 0,   // ยังไม่ตรวจ
    Verified = 1,  // ตรวจแล้ว/ถูกต้อง
    Rejected = 2,  // ไม่ผ่าน/ต้องแก้
}
