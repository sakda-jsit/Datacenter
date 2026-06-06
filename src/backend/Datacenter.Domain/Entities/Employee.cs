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

    public string? Position { get; set; }                      // ตำแหน่ง
    public string? Department { get; set; }                    // ฝ่าย
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
