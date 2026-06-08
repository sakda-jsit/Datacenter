using System.Globalization;
using System.Text;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Wht.DTOs;

namespace Datacenter.Infrastructure.Services.Wht;

/// <summary>
/// ใบแนบ ภ.ง.ด.3 / ภ.ง.ด.53 เป็นไฟล์ TXT (TIS-620, pipe-delimited) สำหรับนำเข้า RD Prep/e-filing.
/// โครงเดียวกับ ภ.ง.ด.1ก + คอลัมน์เฉพาะ WHT: วันที่จ่าย / ประเภทเงินได้ / อัตราภาษี.
/// ที่อยู่ผู้ถูกหักเว้นว่าง (ระบบไม่เก็บที่อยู่ผู้ถูกหักแยกช่อง — RD เติมจากทะเบียนผู้เสียภาษี).
/// </summary>
public class WhtEfilingExportService : IWhtEfilingExportService
{
    static WhtEfilingExportService()
        => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    private static readonly string[] Headers =
    [
        "ลำดับที่", "เลขประจำตัวผู้เสียภาษี", "คำนำหน้าชื่อ", "ชื่อ", "ชื่อกลาง", "นามสกุล",
        "อาคาร", "เลขห้อง", "ชั้น", "หมู่บ้าน", "เลขที่", "หมู่ที่", "ซอย", "แยก", "ถนน",
        "ตำบล", "อำเภอ", "จังหวัด", "รหัสไปรษณีย์",
        "วันเดือนปีที่จ่าย", "ประเภทเงินได้", "อัตราภาษี", "จำนวนเงินที่จ่าย", "จำนวนภาษีที่หัก", "เงื่อนไขการหัก"
    ];

    private static string F(string? s)
        => (s ?? "").Replace("|", " ").Replace("\r", " ").Replace("\n", " ").Trim();
    private static string Amt(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Digits(string? s)
        => string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    public byte[] BuildPndTxt(IReadOnlyList<WhtEntryListItemDto> entries)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join('|', Headers)).Append("\r\n");
        int seq = 1;
        foreach (var e in entries.OrderBy(x => Digits(x.PayeeTaxId), StringComparer.Ordinal))
        {
            var payDate = (e.WithholdDate ?? e.TaxPeriod);
            // วันเดือนปีที่จ่าย = พ.ศ. (dd/MM/yyyy)
            var dateBe = $"{payDate.Day:D2}/{payDate.Month:D2}/{payDate.Year + 543}";
            var cols = new[]
            {
                seq.ToString(CultureInfo.InvariantCulture),
                Digits(e.PayeeTaxId), F(e.PayeePrefix), F(e.PayeeName), "", "",
                "", "", "", "", "", "", "", "", "",   // ที่อยู่ (เว้นว่าง)
                "", "", "", "",
                dateBe, F(e.IncomeType), Amt(e.TaxRate), Amt(e.BaseAmount), Amt(e.TaxAmount),
                "1" // เงื่อนไข: 1 = หัก ณ ที่จ่าย
            };
            sb.Append(string.Join('|', cols)).Append("\r\n");
            seq++;
        }
        return Encoding.GetEncoding(874).GetBytes(sb.ToString());
    }
}
