using Datacenter.Application.Features.Vat.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// สร้างไฟล์โอนย้ายข้อมูล ภ.พ.30 (.txt) สำหรับอัปโหลดหน้า e-Filing กรมสรรพากร
/// ("โอนย้ายข้อมูลแบบแสดงรายการภาษีมูลค่าเพิ่ม ภ.พ.30"). ผู้ใช้แมพคอลัมน์ + เลือก delimiter/header
/// ที่หน้าโอนย้าย ดังนั้นไฟล์เป็น delimited แบบยืดหยุ่น (default = pipe, มี header).
/// </summary>
public interface IVatPp30TransferExportService
{
    byte[] BuildTransferFile(Pp30TransferDto dto, string delimiter, bool includeHeader);
}
