using Datacenter.Application.Features.CashCount.DTOs;
using Datacenter.Domain.Entities;
using DomainCashCount = Datacenter.Domain.Entities.CashCount;

namespace Datacenter.Application.Features.CashCount;

internal static class CashCountMapper
{
    public static decimal CountedTotal(DomainCashCount e)
        => Math.Round(e.Lines.Sum(l => l.Denomination * l.Quantity), 2);

    public static void Apply(DomainCashCount e, CashCountInput d)
    {
        e.FiscalYear = d.FiscalYear;
        e.CountDate = d.CountDate.Date;
        e.Reference = string.IsNullOrWhiteSpace(d.Reference) ? null : d.Reference.Trim();
        e.CashAccountId = d.CashAccountId;
        e.Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim();
        e.AttachmentPath = string.IsNullOrWhiteSpace(d.AttachmentPath) ? null : d.AttachmentPath.Trim();
        e.IsActive = d.IsActive;

        // แทนที่รายการนับทั้งชุด (เก็บเฉพาะที่มีจำนวน > 0) เรียงมูลค่าหน้าตั๋วมากไปน้อย
        e.Lines.Clear();
        var ordered = (d.Lines ?? [])
            .Where(l => l.Quantity > 0 && l.Denomination > 0)
            .OrderByDescending(l => l.Denomination)
            .ToList();
        for (int i = 0; i < ordered.Count; i++)
            e.Lines.Add(new CashCountLine
            {
                Denomination = ordered[i].Denomination,
                Quantity = ordered[i].Quantity,
                SortOrder = i,
            });
    }

    public static CashCountListItemDto ToListItem(DomainCashCount e, IReadOnlyDictionary<int, Account> accounts)
        => new(e.Id, e.FiscalYear, e.CountDate, e.Reference, e.CashAccountId,
            accounts.TryGetValue(e.CashAccountId, out var a) ? a.AccountCode : null,
            CountedTotal(e), e.IsActive);

    public static CashCountDto ToDto(DomainCashCount e, IReadOnlyDictionary<int, Account> accounts)
    {
        var acc = accounts.GetValueOrDefault(e.CashAccountId);
        var lines = e.Lines
            .OrderByDescending(l => l.Denomination)
            .Select(l => new CashCountLineDto(l.Denomination, l.Quantity, Math.Round(l.Denomination * l.Quantity, 2)))
            .ToList();
        return new CashCountDto(
            e.Id, e.ClientCompanyId, e.FiscalYear, e.CountDate, e.Reference,
            e.CashAccountId, acc?.AccountCode, acc?.AccountName,
            e.Notes, e.AttachmentPath, e.IsActive,
            CountedTotal(e), lines);
    }
}
