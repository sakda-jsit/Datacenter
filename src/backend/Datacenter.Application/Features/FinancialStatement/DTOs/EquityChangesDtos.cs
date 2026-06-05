namespace Datacenter.Application.Features.FinancialStatement.DTOs;

/// <summary>หนึ่งองค์ประกอบส่วนของผู้ถือหุ้น (เช่น ทุนที่ชำระแล้ว, กำไรสะสม) ในงบ CAP</summary>
public record EquityComponentDto(
    string RefCode,
    string Name,
    decimal Opening,        // ยอดต้นปี
    decimal NetProfit,      // กำไร(ขาดทุน)สุทธิระหว่างปี (เฉพาะ RE)
    decimal OtherChange,    // รายการเปลี่ยนแปลงอื่น (เพิ่มทุน/เงินปันผล/ปรับปรุง)
    decimal Closing);       // ยอดปลายปี

/// <summary>งบแสดงการเปลี่ยนแปลงส่วนของผู้ถือหุ้น (CAP)</summary>
public record EquityChangesDto(
    int ClientCompanyId,
    string ClientName,
    int FiscalYear,
    IReadOnlyList<EquityComponentDto> Components,
    decimal BalanceSheetEquity)   // ยอดส่วนผู้ถือหุ้นจากงบดุล (ไว้ตรวจว่าตรงกัน)
{
    public decimal TotalOpening => Components.Sum(c => c.Opening);
    public decimal TotalNetProfit => Components.Sum(c => c.NetProfit);
    public decimal TotalOtherChange => Components.Sum(c => c.OtherChange);
    public decimal TotalClosing => Components.Sum(c => c.Closing);
    /// <summary>ยอดปลายปีรวม ตรงกับส่วนผู้ถือหุ้นในงบดุลหรือไม่</summary>
    public bool TiesToBalanceSheet => Math.Abs(TotalClosing - BalanceSheetEquity) < 0.01m;
}
