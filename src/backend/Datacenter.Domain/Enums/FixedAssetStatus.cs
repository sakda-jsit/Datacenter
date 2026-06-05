namespace Datacenter.Domain.Enums;

/// <summary>
/// สถานะสินทรัพย์ถาวรในทะเบียน (req v11 docs/14).
/// - Active: ใช้งานอยู่ — คิดค่าเสื่อมตามปกติ
/// - Disposed: จำหน่าย/ทิ้ง (ไม่มีราคาขาย)
/// - Sold: ขายออก (มีราคาขาย → คำนวณกำไร/ขาดทุนอัตโนมัติ)
/// - WrittenOff: ตัดจำหน่ายออกจากบัญชี
/// สามสถานะหลังหยุดคิดค่าเสื่อมหลังวันที่จำหน่าย/ขาย/ตัดจำหน่าย.
/// </summary>
public enum FixedAssetStatus
{
    Active = 0,
    Disposed = 1,
    Sold = 2,
    WrittenOff = 3,
}
