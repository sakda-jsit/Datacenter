using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ทะเบียนพนักงานของบริษัทลูกค้า (แกนกลางของโมดูลเงินเดือน/ปกส./ภาษี) — กรอกมือ.
/// ข้อมูลส่วนบุคคล (เลขบัตร/วันเกิด/เงินเดือน) = PDPA-sensitive.
/// </summary>
public class Employee : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสพนักงาน (running ต่อบริษัท)</summary>
    public string EmployeeCode { get; set; } = string.Empty;

    public string NationalId { get; set; } = string.Empty;   // เลขบัตรประชาชน
    public string? Prefix { get; set; }                        // คำนำหน้า
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string? MaritalStatus { get; set; }                 // โสด/สมรส
    public string? Nationality { get; set; }
    public string? Address { get; set; }                       // ที่อยู่ (จาก APMAS เมื่อ import) — รวมบรรทัดเดียว

    // ── ที่อยู่แยกช่อง (สำหรับ e-Filing ภ.ง.ด.1ก/1 ของกรมสรรพากร) — กรอกมือ, optional ──
    public string? AddrBuilding { get; set; }                  // อาคาร
    public string? AddrRoomNo { get; set; }                    // เลขที่ห้อง
    public string? AddrFloor { get; set; }                     // ชั้นที่
    public string? AddrVillage { get; set; }                   // หมู่บ้าน
    public string? AddrHouseNo { get; set; }                   // เลขที่
    public string? AddrMoo { get; set; }                       // หมู่ที่
    public string? AddrSoi { get; set; }                       // ซอย/ตรอก
    public string? AddrYaek { get; set; }                      // แยก
    public string? AddrRoad { get; set; }                      // ถนน
    public string? AddrSubDistrict { get; set; }               // ตำบล/แขวง
    public string? AddrDistrict { get; set; }                  // อำเภอ/เขต
    public string? AddrProvince { get; set; }                  // จังหวัด
    public string? AddrPostalCode { get; set; }                // รหัสไปรษณีย์

    public string? Position { get; set; }                      // ตำแหน่ง
    public string? Department { get; set; }                    // ฝ่าย (เช่น ฝ่ายบริหาร/ฝ่ายผลิต — จากบัญชีเงินเดือน Express)

    /// <summary>รหัสเจ้าหนี้ใน Express (APMAS.SUPCOD) — ใช้ match ตอน import จาก Express; ว่าง = กรอกมือ</summary>
    public string? SourceSupplierCode { get; set; }
    public DateTime StartDate { get; set; }                    // วันเริ่มงาน
    public DateTime? ResignDate { get; set; }                  // วันลาออก
    public EmploymentStatus EmploymentStatus { get; set; } = EmploymentStatus.Active;

    public SalaryType SalaryType { get; set; } = SalaryType.Monthly;
    public decimal BaseSalary { get; set; }                    // เงินเดือนฐาน
    public decimal? DailyWage { get; set; }                    // ค่าจ้างรายวัน (ถ้ารายวัน)

    // ประกันสังคม
    public string? SsoNumber { get; set; }                     // เลขผู้ประกันตน (มัก = เลขบัตร)
    public string? SsoHospital { get; set; }                   // โรงพยาบาลตามสิทธิ
    public SsoMemberStatus SsoStatus { get; set; } = SsoMemberStatus.NotEnrolled;

    public string? TaxId { get; set; }                         // เลขผู้เสียภาษี (ถ้าต่างจากเลขบัตร)
    public string? Note { get; set; }

    public ICollection<EmployeeDocument> Documents { get; set; } = [];
    public ICollection<SsoEnrollment> SsoEnrollments { get; set; } = [];
}
