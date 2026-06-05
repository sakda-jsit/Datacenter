using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Stock.Services;

/// <summary>
/// นำเข้าสินค้าคงคลังจาก Express STMAS.DBF — เก็บเฉพาะรายการที่มียอดคงเหลือ/มูลค่า (snapshot).
/// replace ทั้งชุดต่อบริษัท. เรียกใน pipeline นำเข้ากลาง (StartExpressImport). ไม่เรียก SaveChanges.
/// </summary>
public static class StockImporter
{
    public static async Task<(int Read, string Message)> ImportAsync(
        IApplicationDbContext db,
        IExpressDbfAdapter dbfAdapter,
        string folderPath,
        int clientCompanyId,
        int importBatchId,
        string username,
        CancellationToken ct)
    {
        var rows = await dbfAdapter.ReadStockItemsAsync(folderPath, ct);
        // เก็บเฉพาะที่มียอด/มูลค่า (สินค้าที่เคลื่อนไหว) — ตัด master ที่ยอด 0 ออก (ลดสัญญาณรบกวน)
        var meaningful = rows.Where(r => r.OnHandQty != 0 || r.StockValue != 0).ToList();
        if (meaningful.Count == 0)
            return (0, string.Empty);

        var old = await db.StockItems.Where(s => s.ClientCompanyId == clientCompanyId).ToListAsync(ct);
        if (old.Count > 0) db.StockItems.RemoveRange(old);

        foreach (var row in meaningful)
        {
            db.StockItems.Add(new StockItem
            {
                ClientCompanyId = clientCompanyId,
                StockCode    = row.StockCode,
                Name         = row.Name,
                ItemType     = Norm(row.ItemType),
                GroupCode    = Norm(row.GroupCode),
                CategoryCode = Norm(row.CategoryCode),
                Unit         = Norm(row.Unit),
                OnHandQty    = row.OnHandQty,
                UnitCost     = row.UnitCost,
                StockValue   = row.StockValue,
                IsActive     = row.IsActive,
                ImportBatchId = importBatchId,
                CreatedBy    = username,
            });
        }

        var totalValue = meaningful.Sum(r => r.StockValue);
        return (meaningful.Count, $"สินค้าคงคลัง {meaningful.Count} รายการ (มูลค่ารวม {totalValue:#,0.00})");
    }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
