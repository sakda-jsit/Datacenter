namespace Datacenter.Application.Common.Security;

/// <summary>
/// Marker สำหรับ query/command ที่ทำงานกับข้อมูลของบริษัทลูกค้ารายเดียว
/// เมื่อ request implement interface นี้ CompanyAccessBehaviour จะตรวจสิทธิ์
/// ของผู้ใช้ปัจจุบันกับ ClientCompanyId ก่อนเข้าสู่ handler โดยอัตโนมัติ
/// </summary>
public interface IRequireCompanyAccess
{
    int ClientCompanyId { get; }
}
