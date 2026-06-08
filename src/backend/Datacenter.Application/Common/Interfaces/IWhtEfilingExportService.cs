using Datacenter.Application.Features.Wht.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// สร้างไฟล์ใบแนบ ภ.ง.ด.3 / ภ.ง.ด.53 (e-Filing กรมสรรพากร) — pipe-delimited, TIS-620.
/// หมายเหตุ: อ้างอิงรูปแบบ RD Prep ตระกูลเดียวกับ ภ.ง.ด.1ก (ยังไม่ได้ verify กับไฟล์จริง — ควรทดสอบอัปโหลด 1 ครั้ง).
/// </summary>
public interface IWhtEfilingExportService
{
    byte[] BuildPndTxt(IReadOnlyList<WhtEntryListItemDto> entries);
}
