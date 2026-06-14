using System.Globalization;
using System.Text;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Vat.DTOs;

namespace Datacenter.Infrastructure.Services.Vat;

/// <summary>
/// ไฟล์โอนย้ายข้อมูล ภ.พ.30 (.txt, TIS-620) — หนึ่งสาขา = หนึ่งแถว.
/// คอลัมน์ตรงกับช่อง "ข้อมูลการคำนวณภาษี" ในเว็บ RD (ยอดขาย/อัตรา0/ยกเว้น/ยอดซื้อ/ภาษีขาย/ภาษีซื้อ).
/// header/สาขา/เดือน/ประเภทยื่น กรอกบนฟอร์มเว็บ — ในไฟล์ใส่ "สาขา" เป็นคอลัมน์แรกเผื่อกรณียื่นรวมหลายสาขา.
/// ภาษีเดือนนี้/สุทธิ ไม่ใส่ (RD คำนวณให้). จำนวนเงินไม่มี comma, ทศนิยม 2 ตำแหน่ง.
/// </summary>
public class VatPp30TransferExportService : IVatPp30TransferExportService
{
    static VatPp30TransferExportService()
        => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static readonly string[] Headers =
    [
        "สาขาที่", "ยอดขายในเดือนนี้", "ยอดขายอัตราร้อยละ0", "ยอดขายยกเว้น",
        "ยอดซื้อที่มีสิทธิ", "ภาษีขาย", "ภาษีซื้อ"
    ];

    private static string Amt(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

    public byte[] BuildTransferFile(IReadOnlyList<Pp30BranchRow> branches, string delimiter, bool includeHeader)
    {
        var sep = string.IsNullOrEmpty(delimiter) ? "|" : delimiter;

        var sb = new StringBuilder();
        if (includeHeader)
            sb.Append(string.Join(sep, Headers)).Append("\r\n");

        foreach (var b in branches)
        {
            var cols = new[]
            {
                b.BranchNo,
                Amt(b.TotalSales),
                Amt(b.ZeroRatedSales),
                Amt(b.ExemptSales),
                Amt(b.EligiblePurchase),
                Amt(b.OutputVat),
                Amt(b.InputVat),
            };
            sb.Append(string.Join(sep, cols)).Append("\r\n");
        }

        return Encoding.GetEncoding(874).GetBytes(sb.ToString());
    }
}
