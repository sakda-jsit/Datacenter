namespace Datacenter.Domain.Enums;

/// <summary>สถานะการส่งหนังสือรับรองหัก ณ ที่จ่ายทางอีเมล</summary>
public enum WhtEmailStatus
{
    /// <summary>ยังไม่ส่ง</summary>
    NotSent = 0,

    /// <summary>กำลังส่ง</summary>
    Sending = 1,

    /// <summary>ส่งแล้ว</summary>
    Sent = 2,

    /// <summary>ส่งไม่สำเร็จ</summary>
    Failed = 3,
}
