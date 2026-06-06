using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Payroll.DTOs;

namespace Datacenter.Infrastructure.Services.Payroll;

/// <summary>
/// ไฟล์อัปโหลด สปส.1-10 เข้าระบบ e-Service — ชื่อชีต = ลำดับสาขา,
/// คอลัมน์ตามรูปแบบจริง: เลขบัตรประชาชน / คำนำหน้า / ชื่อ / สกุล / ค่าจ้าง / เงินสมทบ
/// </summary>
public class SsoFilingExcelService : ISsoFilingExcelService
{
    private static readonly string[] Headers =
        ["เลขบัตรประชาชน", "คำนำหน้า", "ชื่อ", "สกุล", "ค่าจ้าง", "เงินสมทบ"];

    public byte[] BuildEServiceFile(SsoFilingDto dto)
    {
        using var wb = new XLWorkbook();
        var sheetName = string.IsNullOrWhiteSpace(dto.SsoBranchCode) ? "000000" : dto.SsoBranchCode;
        var ws = wb.Worksheets.Add(sheetName);

        for (int c = 0; c < Headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = Headers[c];
            cell.Style.Font.Bold = true;
        }

        int r = 2;
        foreach (var row in dto.Rows)
        {
            ws.Cell(r, 1).Value = row.NationalId;
            ws.Cell(r, 1).Style.NumberFormat.Format = "@"; // ข้อความ กันตัด 0 หน้า
            ws.Cell(r, 2).Value = row.Prefix;
            ws.Cell(r, 3).Value = row.FirstName;
            ws.Cell(r, 4).Value = row.LastName;
            ws.Cell(r, 5).Value = row.Wage;
            ws.Cell(r, 6).Value = row.Contribution;
            r++;
        }

        ws.Column(1).Width = 18;
        ws.Columns(2, 4).Style.Font.SetBold(false);
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 18;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
