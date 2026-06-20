using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// งานทั่วไป (ad-hoc) ที่มอบหมาย/ติดตามต่อบริษัทลูกค้า — เสริมจาก ComplianceTask (งานภาษีอัตโนมัติ).
/// หัวข้ออิสระ, ผู้รับผิดชอบ, กำหนดส่ง, สถานะ, ความสำคัญ. ทุกคนที่มีสิทธิในบริษัทแก้ได้ (universal edit + audit).
/// </summary>
public class WorkTask : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Open;
    public WorkTaskPriority Priority { get; set; } = WorkTaskPriority.Normal;
    public DateTime? DueDate { get; set; }
    public int? AssignedUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? CompletedByUserId { get; set; }

    /// <summary>งานประจำ: เมื่อปิดงานนี้ (Done) จะ spawn งานถัดไปอัตโนมัติตามรอบ (None = ไม่ซ้ำ)</summary>
    public WorkTaskRecurrence RecurrenceType { get; set; } = WorkTaskRecurrence.None;
    /// <summary>ทุก N หน่วยของรอบ (เช่น Monthly × 3 = ทุกไตรมาส) — ต่ำสุด 1</summary>
    public int RecurrenceInterval { get; set; } = 1;

    public ClientCompany ClientCompany { get; set; } = null!;
    public User? AssignedUser { get; set; }
    public User? CompletedByUser { get; set; }
    public ICollection<WorkTaskItem> Items { get; set; } = [];
}
