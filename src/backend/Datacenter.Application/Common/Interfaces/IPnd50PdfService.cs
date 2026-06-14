using Datacenter.Application.Features.CorporateTax.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// เติมข้อมูลลงแบบ ภ.ง.ด.50 (PDF ฟอร์มราชการ) — overlay ค่าตามพิกัดฟิลด์บน template CIT50.pdf.
/// เฟส A: หน้า 1 (หัว: ชื่อ/เลขผู้เสียภาษี/รอบบัญชี) + หน้า 2 (การคำนวณภาษีจาก TAX engine).
/// </summary>
public interface IPnd50PdfService
{
    byte[] Build(Pnd50FormData data);
}
