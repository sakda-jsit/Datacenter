namespace Datacenter.Domain.Enums;

/// <summary>สถานะการเก็บ snapshot ไฟล์ DBF ต้นฉบับของการนำเข้า (หลักฐานเก็บถาวร 10 ปี)</summary>
public enum ImportSnapshotStatus
{
    /// <summary>เก็บไฟล์ต้นฉบับครบทุกตารางที่พบ</summary>
    Captured = 1,

    /// <summary>เก็บได้บางส่วน (มีบางไฟล์อ่าน/เก็บไม่ได้)</summary>
    Partial = 2,

    /// <summary>เก็บไม่สำเร็จ</summary>
    Failed = 3,
}
