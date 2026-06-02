using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// นิยามรอบบัญชีของบริษัท (ดึงจาก Express ISPRD) — กำหนดงวดและวันสิ้นงวดจริง
/// ใช้เป็น source of truth ว่าปีใด/งวดใดมีอยู่ในระบบ การ import/ลบ/ปิดงวด อ้างอิงตารางนี้
/// PeriodNo = เดือนของ EndDate (1-12) เพื่อให้สอดคล้องกับโมดูลอื่นที่ใช้ Year+Month
/// SourceLocked = ค่า LOCK จาก Express ('Y' = งวดถูกปิด/ล็อกแล้วในต้นทาง)
/// </summary>
public class AccountingPeriod : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public int Year { get; set; }
    public int PeriodNo { get; set; }
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool SourceLocked { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
