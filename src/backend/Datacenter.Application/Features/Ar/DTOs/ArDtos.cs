namespace Datacenter.Application.Features.Ar.DTOs;

public record CustomerDto(
    int Id,
    string CustomerCode,
    string? Prefix,
    string Name,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Contact,
    string? Email,
    int PaymentTermDays,
    string? PaymentCondition,
    string? GlAccountCode,
    bool IsActive,
    decimal OutstandingAmount,   // ยอดค้างรวมของลูกค้า
    int OpenInvoiceCount);

public record ArInvoiceDto(
    int Id,
    string DocumentNo,
    DateTime DocumentDate,
    DateTime? DueDate,
    string CustomerCode,
    string? CustomerName,
    decimal Amount,
    decimal VatAmount,
    decimal NetAmount,
    decimal ReceivedAmount,
    decimal OutstandingAmount,
    bool IsCompleted,
    string? Reference);

/// <summary>หนึ่งบรรทัดในรายงานอายุหนี้ (ต่อลูกค้า)</summary>
public record ArAgingRowDto(
    string CustomerCode,
    string CustomerName,
    decimal NotDue,        // ยังไม่ถึงกำหนด
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus,
    decimal Total);

public record ArAgingReportDto(
    int ClientCompanyId,
    string ClientName,
    DateTime AsOfDate,
    IReadOnlyList<ArAgingRowDto> Rows,
    DateTime? DataAsOf = null)   // เวลานำเข้าใบแจ้งหนี้ลูกหนี้ล่าสุด (snapshot)
{
    public decimal TotalNotDue => Rows.Sum(r => r.NotDue);
    public decimal TotalDays1To30 => Rows.Sum(r => r.Days1To30);
    public decimal TotalDays31To60 => Rows.Sum(r => r.Days31To60);
    public decimal TotalDays61To90 => Rows.Sum(r => r.Days61To90);
    public decimal TotalDays90Plus => Rows.Sum(r => r.Days90Plus);
    public decimal GrandTotal => Rows.Sum(r => r.Total);
}
