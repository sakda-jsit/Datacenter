namespace Datacenter.Application.Features.ComplianceCalendar.Services;

/// <summary>
/// ค่าคงที่สำหรับผูกเอกสารแนบ (Attachment) เข้ากับงานประจำรายงวด (ComplianceTask).
/// Attachment เป็น polymorphic อยู่แล้ว (ModuleName + RecordId) จึงไม่ต้องมีตารางเพิ่ม —
/// หลักฐานการยื่นของงวดหนึ่งคือ Attachment ที่ ModuleName = "ComplianceTask" และ RecordId = TaskId
/// </summary>
public static class ComplianceEvidence
{
    public const string ModuleName = "ComplianceTask";
}
