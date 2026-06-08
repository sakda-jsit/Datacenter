using System.Globalization;
using ClosedXML.Excel;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Vat.DTOs;

namespace Datacenter.Infrastructure.Services.Vat;

/// <summary>
/// รายงานภาษีขาย (VatType=1) / รายงานภาษีซื้อ (VatType=2) เป็น Excel — รูปแบบรายงานภาษีตามประมวลรัษฎากร
/// (หัวรายงาน: ชื่อผู้ประกอบการ/เลขผู้เสียภาษี/เดือนภาษี/สถานประกอบการ + คอลัมน์มาตรฐาน + ยอดรวม).
/// </summary>
public class VatTaxReportExportService : IVatTaxReportExportService
{
    private static readonly string[] MonthsTh =
        ["", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
         "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"];

    public byte[] BuildExcel(VatTaxReportDto d)
    {
        bool isSales = d.VatType == 1;
        string title = isSales ? "รายงานภาษีขาย" : "รายงานภาษีซื้อ";
        string partyLabel = isSales ? "ชื่อผู้ซื้อสินค้า/ผู้รับบริการ" : "ชื่อผู้ขายสินค้า/ผู้ให้บริการ";
        string period = d.Month > 0 ? $"{MonthsTh[d.Month]} {d.Year + 543}" : $"ปี {d.Year + 543}";

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(isSales ? "ภาษีขาย" : "ภาษีซื้อ");

        // หัวรายงาน
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"ชื่อผู้ประกอบการ: {d.CompanyName}    เลขประจำตัวผู้เสียภาษีอากร: {d.TaxId}";
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(3, 1).Value = $"เดือนภาษี: {period}    สถานประกอบการ: สำนักงานใหญ่";
        ws.Range(3, 1, 3, 7).Merge();

        // หัวคอลัมน์
        const int hr = 5;
        var headers = new[]
        {
            "ลำดับ", "วัน/เดือน/ปี", "เลขที่ใบกำกับภาษี", partyLabel,
            "เลขประจำตัวผู้เสียภาษี", "มูลค่าสินค้า/บริการ", "จำนวนภาษีมูลค่าเพิ่ม"
        };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(hr, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        int r = hr + 1;
        foreach (var row in d.Rows)
        {
            ws.Cell(r, 1).Value = row.Seq;
            ws.Cell(r, 2).Value = row.Date?.ToString("dd/MM/", CultureInfo.InvariantCulture) + (row.Date is { } dt ? (dt.Year + 543).ToString() : "");
            ws.Cell(r, 3).Value = row.DocNo;
            ws.Cell(r, 4).Value = row.Name ?? "";
            ws.Cell(r, 5).Value = row.TaxId ?? "";
            ws.Cell(r, 6).Value = row.BaseAmount;
            ws.Cell(r, 7).Value = row.VatAmount;
            ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
            for (int c = 1; c <= 7; c++) ws.Cell(r, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            r++;
        }

        // ยอดรวม
        var totalCell = ws.Cell(r, 5);
        totalCell.Value = "รวม";
        totalCell.Style.Font.Bold = true;
        totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(r, 6).Value = d.TotalBase;
        ws.Cell(r, 7).Value = d.TotalVat;
        ws.Cell(r, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(r, 7).Style.NumberFormat.Format = "#,##0.00";
        for (int c = 1; c <= 7; c++) ws.Cell(r, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(r, 6, r, 7).Style.Font.Bold = true;

        if (isSales && d.TotalZeroRated != 0)
            ws.Cell(r + 1, 5).Value = "ยอดขายอัตรา 0%";
        if (isSales && d.TotalZeroRated != 0)
        {
            ws.Cell(r + 1, 6).Value = d.TotalZeroRated;
            ws.Cell(r + 1, 6).Style.NumberFormat.Format = "#,##0.00";
        }

        ws.Columns().AdjustToContents();
        ws.Column(4).Width = 36;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
