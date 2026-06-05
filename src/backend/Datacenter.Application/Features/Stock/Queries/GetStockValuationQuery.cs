using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Stock.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Stock.Queries;

/// <summary>
/// รายงานมูลค่าสินค้าคงเหลือ + สรุปตามกลุ่ม + เทียบบัญชีสินค้าคงเหลือใน GL (FG↔TB).
/// FiscalYear ใช้คำนวณยอด GL สะสมถึงสิ้นปี (ยอดสินค้าใน STMAS เป็น snapshot ปัจจุบัน).
/// </summary>
public record GetStockValuationQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<StockValuationDto>, IRequireCompanyAccess;

public class GetStockValuationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStockValuationQuery, StockValuationDto>
{
    public async Task<StockValuationDto> Handle(GetStockValuationQuery request, CancellationToken ct)
    {
        var clientName = await db.ClientCompanies
            .AsNoTracking().Where(c => c.Id == request.ClientCompanyId)
            .Select(c => c.LegalName).FirstOrDefaultAsync(ct) ?? "";

        var items = await db.StockItems
            .AsNoTracking()
            .Where(s => s.ClientCompanyId == request.ClientCompanyId)
            .OrderByDescending(s => s.StockValue).ThenBy(s => s.StockCode)
            .Select(s => new StockItemDto(
                s.Id, s.StockCode, s.Name, s.ItemType, s.GroupCode, s.Unit, s.OnHandQty, s.UnitCost, s.StockValue))
            .ToListAsync(ct);

        // ความสดของข้อมูล: เวลาที่นำเข้าสินค้าคงคลังล่าสุด (StockItem ถูก replace ทุกครั้งที่ import)
        DateTime? dataAsOf = await db.StockItems
            .AsNoTracking()
            .Where(s => s.ClientCompanyId == request.ClientCompanyId)
            .MaxAsync(s => (DateTime?)s.CreatedAt, ct);

        var groups = items
            .GroupBy(i => string.IsNullOrWhiteSpace(i.GroupCode) ? "(ไม่ระบุกลุ่ม)" : i.GroupCode!)
            .Select(g => new StockGroupSummaryDto(g.Key, g.Count(), Math.Round(g.Sum(x => x.StockValue), 2)))
            .OrderByDescending(g => g.TotalValue)
            .ToList();

        // บัญชีสินค้าคงเหลือใน GL (ชื่อมีคำว่า "สินค้าคงเหลือ") — ยอดสะสมถึงสิ้นปีงบ (สินทรัพย์: debit − credit)
        var yearEndExclusive = new DateTime(request.FiscalYear, 12, 31).AddDays(1);
        // รองรับทั้งผังบัญชีไทย ("สินค้าคงเหลือ") และอังกฤษ ("Inventory")
        var invAccounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId
                     && (a.AccountName.Contains("สินค้าคงเหลือ") || a.AccountName.Contains("Inventory")))
            .Select(a => new { a.Id, a.AccountCode, a.AccountName })
            .ToListAsync(ct);

        var glAccounts = new List<StockGlCompareDto>();
        if (invAccounts.Count > 0)
        {
            var ids = invAccounts.Select(a => a.Id).ToList();
            var bal = await db.JournalEntryLines
                .AsNoTracking()
                .Where(l => l.JournalEntry.ClientCompanyId == request.ClientCompanyId
                         && l.JournalEntry.JournalDate < yearEndExclusive
                         && ids.Contains(l.AccountId))
                .GroupBy(l => l.AccountId)
                .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.DebitAmount), Credit = g.Sum(x => x.CreditAmount) })
                .ToDictionaryAsync(x => x.AccountId, ct);

            foreach (var a in invAccounts)
            {
                var n = bal.GetValueOrDefault(a.Id);
                var glBal = Math.Round((n?.Debit ?? 0m) - (n?.Credit ?? 0m), 2);
                glAccounts.Add(new StockGlCompareDto(a.Id, a.AccountCode, a.AccountName, glBal));
            }
        }

        return new StockValuationDto(request.ClientCompanyId, clientName, request.FiscalYear, items, groups, glAccounts, dataAsOf);
    }
}
