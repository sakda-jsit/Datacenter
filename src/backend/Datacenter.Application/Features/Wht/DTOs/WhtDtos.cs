namespace Datacenter.Application.Features.Wht.DTOs;

/// <summary>สรุปภาษีหัก ณ ที่จ่ายหนึ่งเดือน (แยก ภ.ง.ด.3 / ภ.ง.ด.53)</summary>
public record WhtMonthlyDto(
    int Month,
    decimal Pnd3Base,      // ฐานเงินได้ ภ.ง.ด.3 (บุคคลธรรมดา)
    decimal Pnd3Tax,       // ภาษีหัก ภ.ง.ด.3
    int Pnd3Count,
    decimal Pnd53Base,     // ฐานเงินได้ ภ.ง.ด.53 (นิติบุคคล)
    decimal Pnd53Tax,      // ภาษีหัก ภ.ง.ด.53
    int Pnd53Count)
{
    public decimal TotalTax => Math.Round(Pnd3Tax + Pnd53Tax, 2);
}

/// <summary>รายงานภาษีหัก ณ ที่จ่ายรายเดือนตลอดปีปฏิทิน + ยอดรวมทั้งปี</summary>
public record WhtReportDto(
    int ClientCompanyId,
    string ClientName,
    int Year,
    IReadOnlyList<WhtMonthlyDto> Months,
    DateTime? DataAsOf = null)   // เวลานำเข้า WHT ล่าสุด (snapshot — ไม่ใช่ real-time)
{
    public decimal TotalPnd3Base => Months.Sum(m => m.Pnd3Base);
    public decimal TotalPnd3Tax => Months.Sum(m => m.Pnd3Tax);
    public int TotalPnd3Count => Months.Sum(m => m.Pnd3Count);
    public decimal TotalPnd53Base => Months.Sum(m => m.Pnd53Base);
    public decimal TotalPnd53Tax => Months.Sum(m => m.Pnd53Tax);
    public int TotalPnd53Count => Months.Sum(m => m.Pnd53Count);
    public decimal TotalTax => Math.Round(TotalPnd3Tax + TotalPnd53Tax, 2);
}

/// <summary>หนึ่งรายการในรายละเอียดภาษีหัก ณ ที่จ่าย</summary>
public record WhtEntryListItemDto(
    int Id,
    int FormType,          // 3=ภ.ง.ด.3, 53=ภ.ง.ด.53
    DateTime TaxPeriod,
    DateTime? WithholdDate,
    string DocumentNo,
    string? PayeeName,
    string? PayeePrefix,
    string? PayeeTaxId,
    string? IncomeType,
    decimal BaseAmount,
    decimal TaxRate,
    decimal TaxAmount,
    bool IsLate,
    // ── อีเมล/สถานะการส่งหนังสือรับรอง ──
    string? PayeeEmail,
    int EmailStatus,       // 0=ยังไม่ส่ง, 1=กำลังส่ง, 2=ส่งแล้ว, 3=ส่งไม่สำเร็จ
    DateTime? EmailSentAt,
    string? EmailSentBy,
    string? EmailError);

/// <summary>ผู้ถูกหัก (สำหรับจัดการอีเมล)</summary>
public record WhtPayeeDto(
    string TaxId,
    string? Name,
    string? Email,
    int EntryCount);

/// <summary>ผลการส่งอีเมลต่อผู้ถูกหักหนึ่งราย</summary>
public record WhtSendResultDto(
    string PayeeTaxId,
    string? PayeeName,
    string? Email,
    bool Success,
    int EntryCount,
    string? Error);
