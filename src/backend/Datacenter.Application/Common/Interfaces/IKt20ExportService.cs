using Datacenter.Application.Features.Payroll.DTOs;

namespace Datacenter.Application.Common.Interfaces;

/// <summary>สร้างแบบ กท.20ก (แบบแสดงเงินค่าจ้างประจำปี กองทุนเงินทดแทน): Excel + PDF + รูป preview</summary>
public interface IKt20ExportService
{
    byte[] BuildExcel(Kt20Dto dto);
    byte[] BuildPdf(Kt20Dto dto);
    IReadOnlyList<byte[]> BuildImages(Kt20Dto dto);
}
