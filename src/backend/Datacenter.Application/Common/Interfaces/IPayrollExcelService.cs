namespace Datacenter.Application.Common.Interfaces;

/// <summary>หนึ่งแถวพนักงานใน Excel งวดเงินเดือน (รหัสพนักงานใช้เป็นคีย์ตอน import)</summary>
public record PayrollExcelRow(
    string EmployeeCode,
    string EmployeeName,
    decimal Salary,
    decimal DailyWageDays,
    decimal DailyWageRate,
    decimal HousingAllowance,
    decimal FoodAllowance,
    decimal Overtime,
    decimal Diligence,
    decimal Bonus,
    decimal OtherIncome,
    decimal SsoWageBase,
    decimal SsoEmployee,
    decimal WithholdingTax,
    decimal Absence,
    decimal Advance,
    decimal OtherDeduction);

/// <summary>สร้าง/อ่าน Excel template งวดเงินเดือน (กรอกรายได้+รายการหักนอกระบบแล้วอัปโหลด)</summary>
public interface IPayrollExcelService
{
    /// <summary>สร้าง template (หัว + รายชื่อพนักงาน + ค่าปัจจุบัน) ให้ดาวน์โหลดไปกรอก</summary>
    byte[] BuildTemplate(int year, int month, string companyName, IReadOnlyList<PayrollExcelRow> rows);

    /// <summary>อ่านไฟล์ที่อัปโหลด → แถวข้อมูล (คีย์ด้วยรหัสพนักงาน)</summary>
    IReadOnlyList<PayrollExcelRow> Parse(byte[] file);
}
