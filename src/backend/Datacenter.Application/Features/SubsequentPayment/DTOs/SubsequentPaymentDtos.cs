namespace Datacenter.Application.Features.SubsequentPayment.DTOs;

/// <summary>
/// หนึ่งบรรทัดจ่ายชำระจริงในปีถัดไป (อ้างอิง GLJNLIT สดจาก Express) — เป็นหลักฐานประกอบเท่านั้น
/// </summary>
public record SubsequentPaymentDetailDto(
    string Voucher,
    DateTime? Date,
    string Description,
    decimal Amount);

/// <summary>
/// หนึ่งบัญชีค้างจ่าย ณ สิ้นปีปิดงบ + การจ่ายชำระจริงที่พบในปีถัดไป
/// Status: paid (จ่ายครอบคลุมยอดค้าง) / partial (จ่ายบางส่วน) / unpaid (ยังไม่พบการจ่าย) / unmatched (ตรวจปีถัดไปไม่ได้)
/// </summary>
public record SubsequentPaymentRowDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal YearEndPayable,      // ยอดค้างจ่าย ณ สิ้นปีปิดงบ (เครดิต-เป็นบวก จาก GL ที่นำเข้า)
    decimal SubsequentPaid,      // ยอดจ่ายชำระ (เดบิต) ที่พบในปีถัดไป
    decimal Remaining,           // คงเหลือยังไม่พบการจ่าย = max(YearEndPayable − SubsequentPaid, 0)
    string Status,
    IReadOnlyList<SubsequentPaymentDetailDto> Payments);

/// <summary>
/// รายงานตรวจการจ่ายชำระหลังปิดงบ (RPT-019) — ดูว่ารายการค้างจ่ายปีปิดงบ ถูกจ่ายจริงในปีถัดไปหรือยัง
/// ข้อมูลปีถัดไปเป็น "หลักฐานประกอบ (subsequent evidence)" เท่านั้น ไม่นำมารวมยอดปีปิดงบ
/// </summary>
public record SubsequentPaymentReportDto(
    int ClientCompanyId,
    string ClientCode,
    string ClientName,
    int FiscalYear,
    int SubsequentYear,
    bool ExpressAvailable,       // อ่าน GLJNLIT ปีถัดไปจาก Express ได้หรือไม่
    DateTime CheckedAt,          // เวลาตรวจสด (UTC)
    IReadOnlyList<SubsequentPaymentRowDto> Rows)
{
    public decimal TotalYearEndPayable => Rows.Sum(r => r.YearEndPayable);
    public decimal TotalSubsequentPaid => Rows.Sum(r => r.SubsequentPaid);
    public decimal TotalRemaining => Rows.Sum(r => r.Remaining);
    public int PaidCount => Rows.Count(r => r.Status == "paid");
    public int PartialCount => Rows.Count(r => r.Status == "partial");
    public int UnpaidCount => Rows.Count(r => r.Status == "unpaid");
}
