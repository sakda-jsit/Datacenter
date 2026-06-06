using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;

namespace Datacenter.Infrastructure.Services.Payroll;

/// <summary>สร้าง/อ่าน Excel งวดเงินเดือนด้วย ClosedXML — คอลัมน์ A=รหัส (คีย์), B=ชื่อ, C–P=ตัวเลข</summary>
public class PayrollExcelService : IPayrollExcelService
{
    // ลำดับคอลัมน์ค่าตัวเลข (C..P) ตรงกับ PayrollExcelRow
    private static readonly string[] Headers =
    [
        "รหัสพนักงาน", "ชื่อ-สกุล",
        "เงินเดือน", "วันทำงาน", "ค่าจ้าง/วัน", "ค่าที่พัก", "ค่าอาหาร", "ค่าล่วงเวลา",
        "เบี้ยขยัน", "โบนัส", "รายได้อื่น",
        "ฐานยื่น ปกส.", "หัก ปกส.", "ภาษีหัก ณ ที่จ่าย", "ขาดงาน", "หักอื่นๆ",
    ];

    public byte[] BuildTemplate(int year, int month, string companyName, IReadOnlyList<PayrollExcelRow> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("งวดเงินเดือน");

        ws.Cell(1, 1).Value = $"งวดเงินเดือน {month}/{year} — {companyName}";
        ws.Range(1, 1, 1, Headers.Length).Merge().Style.Font.SetBold().Font.FontSize = 13;

        for (int c = 0; c < Headers.Length; c++)
        {
            var cell = ws.Cell(2, c + 1);
            cell.Value = Headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.WrapText = true;
        }

        int r = 3;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.EmployeeCode;
            ws.Cell(r, 2).Value = row.EmployeeName;
            decimal[] vals =
            [
                row.Salary, row.DailyWageDays, row.DailyWageRate, row.HousingAllowance, row.FoodAllowance,
                row.Overtime, row.Diligence, row.Bonus, row.OtherIncome,
                row.SsoWageBase, row.SsoEmployee, row.WithholdingTax, row.Absence, row.OtherDeduction,
            ];
            for (int i = 0; i < vals.Length; i++)
                ws.Cell(r, 3 + i).Value = vals[i];
            r++;
        }

        // ล็อกคอลัมน์รหัส/ชื่อ ให้ผู้กรอกไม่แก้ (ใช้เป็นคีย์), เปิดให้แก้คอลัมน์ตัวเลข
        ws.Columns(1, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(245, 245, 245);
        ws.Column(1).Width = 14;
        ws.Column(2).Width = 26;
        for (int c = 3; c <= Headers.Length; c++) ws.Column(c).Width = 11;
        ws.SheetView.FreezeRows(2);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public IReadOnlyList<PayrollExcelRow> Parse(byte[] file)
    {
        using var ms = new MemoryStream(file);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First();
        var result = new List<PayrollExcelRow>();

        // ข้อมูลเริ่มแถว 3 (แถว 1 = หัวเรื่อง, แถว 2 = หัวคอลัมน์)
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
        for (int r = 3; r <= lastRow; r++)
        {
            var code = ws.Cell(r, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(code)) continue;
            decimal D(int c) => Dec(ws.Cell(r, c));
            result.Add(new PayrollExcelRow(
                code, ws.Cell(r, 2).GetString().Trim(),
                D(3), D(4), D(5), D(6), D(7), D(8), D(9), D(10), D(11),
                D(12), D(13), D(14), D(15), D(16)));
        }
        return result;
    }

    private static decimal Dec(IXLCell cell)
    {
        if (cell.IsEmpty()) return 0m;
        if (cell.TryGetValue<decimal>(out var d)) return d;
        return decimal.TryParse(cell.GetString().Replace(",", "").Trim(), out var p) ? p : 0m;
    }
}
