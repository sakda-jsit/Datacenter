namespace Datacenter.Domain.Enums;

/// <summary>
/// สถานะชุดรายงานงบการเงิน (req v11 #9):
/// Draft → Review → Final (snapshot ยอด+ชื่อบริษัท ณ ตอน finalize) → Locked (ยื่นแล้ว ห้ามแก้).
/// ปลดล็อก (Locked → Final) ได้โดยผู้มีสิทธิ์ในบริษัท + audit. ยื่นเพิ่มเติม = เปิด version ใหม่.
/// </summary>
public enum ReportPackageStatus
{
    Draft = 0,
    Review = 1,
    Final = 2,
    Locked = 3,
}
