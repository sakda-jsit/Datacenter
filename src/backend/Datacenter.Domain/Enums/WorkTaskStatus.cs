namespace Datacenter.Domain.Enums;

/// <summary>สถานะงานทั่วไป (ad-hoc) — แยกจาก ComplianceTaskStatus (เกินกำหนด = คำนวณสด ไม่เก็บเป็นสถานะ)</summary>
public enum WorkTaskStatus
{
    Open = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3,
}
