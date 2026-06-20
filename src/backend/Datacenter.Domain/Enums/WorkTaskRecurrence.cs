namespace Datacenter.Domain.Enums;

/// <summary>รอบการเกิดซ้ำของงาน (recurring) — เมื่อปิดงานที่ตั้งซ้ำ จะสร้างงานถัดไปอัตโนมัติ</summary>
public enum WorkTaskRecurrence
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Yearly = 4,
}
