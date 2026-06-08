using Datacenter.Application.Features.Vat.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>สร้างรายงานภาษีขาย / รายงานภาษีซื้อ (Excel) สำหรับยื่น ภ.พ.30 + เก็บหลักฐาน</summary>
public interface IVatTaxReportExportService
{
    byte[] BuildExcel(VatTaxReportDto dto);
}
