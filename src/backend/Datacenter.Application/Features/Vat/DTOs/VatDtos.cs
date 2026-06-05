namespace Datacenter.Application.Features.Vat.DTOs;

/// <summary>สรุปภาษีมูลค่าเพิ่มหนึ่งเดือน (หนึ่งงวด ภ.พ.30)</summary>
public record VatMonthlyDto(
    int Month,
    decimal OutputBase,        // ยอดขายที่ต้องเสียภาษี
    decimal OutputVat,         // ภาษีขาย
    decimal OutputZeroRated,   // ยอดขายอัตรา 0%
    int OutputCount,
    decimal InputBase,         // ยอดซื้อที่มีสิทธิ์ขอคืน
    decimal InputVat,          // ภาษีซื้อ
    int InputCount)
{
    /// <summary>ภาษีที่ต้องชำระ (&gt;0) หรือ ชำระเกิน/ยกไป (&lt;0) = ภาษีขาย − ภาษีซื้อ</summary>
    public decimal NetVat => Math.Round(OutputVat - InputVat, 2);
}

/// <summary>รายงาน ภ.พ.30 รายเดือนตลอดปีปฏิทิน + ยอดรวมทั้งปี</summary>
public record VatReportDto(
    int ClientCompanyId,
    string ClientName,
    int Year,
    IReadOnlyList<VatMonthlyDto> Months,
    DateTime? DataAsOf = null)   // เวลานำเข้า VAT ล่าสุด (snapshot — ไม่ใช่ real-time)
{
    public decimal TotalOutputBase => Months.Sum(m => m.OutputBase);
    public decimal TotalOutputVat => Months.Sum(m => m.OutputVat);
    public decimal TotalOutputZeroRated => Months.Sum(m => m.OutputZeroRated);
    public decimal TotalInputBase => Months.Sum(m => m.InputBase);
    public decimal TotalInputVat => Months.Sum(m => m.InputVat);
    public decimal TotalNetVat => Math.Round(TotalOutputVat - TotalInputVat, 2);
    public int TotalOutputCount => Months.Sum(m => m.OutputCount);
    public int TotalInputCount => Months.Sum(m => m.InputCount);
}

/// <summary>หนึ่งรายการในรายละเอียดภาษีซื้อ/ขาย</summary>
public record VatEntryListItemDto(
    int Id,
    int VatType,               // 1=ขาย(Output), 2=ซื้อ(Input)
    DateTime TaxPeriod,
    DateTime? DocumentDate,
    string DocumentNo,
    string? ReferenceNo,
    string? Description,
    string? CounterpartyTaxId,
    string? CounterpartyPrefix,
    decimal BaseAmount,
    decimal VatAmount,
    decimal ZeroRatedAmount,
    bool IsLate);
