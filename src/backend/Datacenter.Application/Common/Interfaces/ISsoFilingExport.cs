using Datacenter.Application.Features.Payroll.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>สร้างไฟล์ Excel อัปโหลดเข้าระบบ e-Service ของ ปกส. (รูปแบบ: เลขบัตร/คำนำหน้า/ชื่อ/สกุล/ค่าจ้าง/เงินสมทบ)</summary>
public interface ISsoFilingExcelService
{
    byte[] BuildEServiceFile(SsoFilingDto dto);
}

/// <summary>สร้าง PDF ฟอร์ม สปส.1-10 (ส่วนที่ 1 หน้าปก + ส่วนที่ 2 รายชื่อ)</summary>
public interface ISsoFilingPdfService
{
    byte[] Generate(SsoFilingDto dto);
}
