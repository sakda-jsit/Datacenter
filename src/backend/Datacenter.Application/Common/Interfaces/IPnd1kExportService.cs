using Datacenter.Application.Features.Payroll.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>สร้างไฟล์ ภ.ง.ด.1ก (ใบแนบ): Excel + PDF</summary>
public interface IPnd1kExportService
{
    byte[] BuildExcel(Pnd1kDto dto);
    byte[] BuildPdf(Pnd1kDto dto);
}
