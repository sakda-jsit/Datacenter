using Datacenter.Application.Features.FinancialStatement.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// สร้างไฟล์ Excel หมายเหตุประกอบงบการเงิน (NOTE2) รูปแบบเอกสารงบที่ยื่น —
/// เลียนแบบ sheet NOTE2 ของ workbook อ้างอิง (ฟอนต์ AngsanaUPC, หัว+ลงชื่อทุกหน้า,
/// ตัวเลขแบบบัญชี, ตารางการเคลื่อนไหวมีกรอบ, แบ่งหน้า A4).
/// </summary>
public interface INote2ExcelExporter
{
    byte[] Build(NotesToFsDto data, string directorName);
}
