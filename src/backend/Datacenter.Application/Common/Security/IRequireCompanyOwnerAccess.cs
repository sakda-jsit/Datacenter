namespace Datacenter.Application.Common.Security;

/// <summary>
/// Marker สำหรับ request ที่ต้องเป็น <b>ผู้ดูแลบริษัทนั้น</b> (มีแถวใน CompanyUserAccess) จึงจะทำได้
/// — เข้มกว่า <see cref="IRequireCompanyAccess"/> ซึ่งเป็นระดับ "ดูอย่างเดียว" ที่ผู้ใช้ทุกคนผ่าน
///
/// ใช้กับ:
/// 1. <b>ทุก command</b> ที่แก้ไขข้อมูลของบริษัท (บันทึก/แก้/ลบ/นำเข้า/ปิดงวด)
/// 2. <b>query ของโมดูลเงินเดือน</b> เพราะมีข้อมูลส่วนบุคคลตาม PDPA
///    (เลขบัตรประชาชน เงินเดือนรายคน เอกสารแนบพนักงาน) — คนที่ไม่ได้ดูแลบริษัทนี้ไม่ควรเห็น
///
/// Admin ผ่านทุกกรณี
/// </summary>
public interface IRequireCompanyOwnerAccess : IRequireCompanyAccess
{
}
