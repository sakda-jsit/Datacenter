namespace Datacenter.Application.Features.Ap.DTOs;

public record SupplierDto(
    int Id,
    string SupplierCode,
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
    decimal OutstandingAmount,
    int OpenInvoiceCount);

public record ApInvoiceDto(
    int Id,
    string DocumentNo,
    DateTime DocumentDate,
    DateTime? DueDate,
    string SupplierCode,
    string? SupplierName,
    decimal Amount,
    decimal VatAmount,
    decimal NetAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    bool IsCompleted,
    string? Reference);

/// <summary>หนึ่งบรรทัดในรายงานอายุหนี้เจ้าหนี้ (ต่อผู้ขาย)</summary>
public record ApAgingRowDto(
    string SupplierCode,
    string SupplierName,
    decimal NotDue,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus,
    decimal Total);

public record ApAgingReportDto(
    int ClientCompanyId,
    string ClientName,
    DateTime AsOfDate,
    IReadOnlyList<ApAgingRowDto> Rows)
{
    public decimal TotalNotDue => Rows.Sum(r => r.NotDue);
    public decimal TotalDays1To30 => Rows.Sum(r => r.Days1To30);
    public decimal TotalDays31To60 => Rows.Sum(r => r.Days31To60);
    public decimal TotalDays61To90 => Rows.Sum(r => r.Days61To90);
    public decimal TotalDays90Plus => Rows.Sum(r => r.Days90Plus);
    public decimal GrandTotal => Rows.Sum(r => r.Total);
}
