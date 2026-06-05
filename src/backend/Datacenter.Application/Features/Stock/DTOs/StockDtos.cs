namespace Datacenter.Application.Features.Stock.DTOs;

public record StockItemDto(
    int Id,
    string StockCode,
    string Name,
    string? ItemType,
    string? GroupCode,
    string? Unit,
    decimal OnHandQty,
    decimal UnitCost,
    decimal StockValue);

/// <summary>สรุปมูลค่าสินค้าตามกลุ่ม</summary>
public record StockGroupSummaryDto(
    string GroupCode,
    int Count,
    decimal TotalValue);

/// <summary>เทียบมูลค่าสินค้าคงเหลือกับบัญชีสินค้าคงเหลือใน GL (FG↔TB)</summary>
public record StockGlCompareDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal GlBalance);

public record StockValuationDto(
    int ClientCompanyId,
    string ClientName,
    int FiscalYear,
    IReadOnlyList<StockItemDto> Items,
    IReadOnlyList<StockGroupSummaryDto> Groups,
    IReadOnlyList<StockGlCompareDto> GlAccounts,
    DateTime? DataAsOf)   // เวลาที่นำเข้าสินค้าคงคลังล่าสุด (snapshot ไม่ใช่ real-time)
{
    public decimal TotalStockValue => Items.Sum(i => i.StockValue);
    public decimal TotalGlBalance => GlAccounts.Sum(g => g.GlBalance);
    /// <summary>ผลต่าง = มูลค่าสินค้า(STMAS) − ยอดบัญชีสินค้าคงเหลือ(GL) → ให้บัญชีบันทึก adjustment เอง</summary>
    public decimal Difference => Math.Round(TotalStockValue - TotalGlBalance, 2);
    public bool HasGlAccounts => GlAccounts.Count > 0;
}
