using Datacenter.Application.Features.Payroll.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>สร้างไฟล์ ภ.ง.ด.1ก (ใบแนบ): Excel + PDF + TXT (e-Filing กรมสรรพากร)</summary>
public interface IPnd1kExportService
{
    byte[] BuildExcel(Pnd1kDto dto);
    byte[] BuildPdf(Pnd1kDto dto);

    /// <summary>ไฟล์ข้อความ pipe-delimited (TIS-620) สำหรับนำเข้าโปรแกรมยื่นแบบ/RD Prep ภ.ง.ด.1ก</summary>
    byte[] BuildTxt(Pnd1kDto dto);
}
