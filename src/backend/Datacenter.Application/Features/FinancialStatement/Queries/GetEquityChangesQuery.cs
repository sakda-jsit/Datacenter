using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Application.Features.FinancialStatement.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>งบแสดงการเปลี่ยนแปลงส่วนของผู้ถือหุ้น (CAP) สำหรับปีงบที่ระบุ</summary>
public record GetEquityChangesQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<EquityChangesDto>, IRequireCompanyAccess;

public class GetEquityChangesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetEquityChangesQuery, EquityChangesDto>
{
    public async Task<EquityChangesDto> Handle(GetEquityChangesQuery request, CancellationToken ct)
    {
        var client = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var allLines = await db.StatementLines.AsNoTracking().OrderBy(l => l.SortOrder).ToListAsync(ct);
        var mappings = await db.AccountStatementMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .ToDictionaryAsync(m => m.AccountCode, ct);
        var accounts = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId && a.IsActive)
            .ToDictionaryAsync(a => a.AccountCode, ct);

        var yearStart = new DateTime(request.FiscalYear, 1, 1);          // ต้นปี (exclusive ขอบบนของยอดต้นปี)
        var yearEnd = new DateTime(request.FiscalYear + 1, 1, 1);        // สิ้นปี (exclusive)

        var openingNets = await GetAccountNetsAsync(db, request.ClientCompanyId, yearStart, ct);   // สะสมถึงสิ้นปีก่อน
        var closingNets = await GetAccountNetsAsync(db, request.ClientCompanyId, yearEnd, ct);      // สะสมถึงสิ้นปีนี้

        // กำไรสุทธิปีนี้ (ฐานเดียวกับงบดุล/งบกำไรขาดทุน)
        var taxInputs = await db.FsExternalInputs.AsNoTracking()
            .Where(x => x.ClientCompanyId == request.ClientCompanyId && x.FiscalYear == request.FiscalYear
                     && (x.RefCode == "X4" || x.RefCode == "WHT"))
            .ToDictionaryAsync(x => x.RefCode, x => x.Amount, ct);
        decimal externalTax = taxInputs.GetValueOrDefault("X4");

        var plResult = FinancialStatementEngine.BuildProfitLoss(
            client, request.FiscalYear, null, null, allLines, closingNets, mappings, accounts, externalTax);
        decimal netProfit = plResult.NetProfit;

        // โยงบัญชี → refcode ส่วนผู้ถือหุ้น (Section 'E')
        var equityLines = allLines.Where(l => l.Section == 'E').OrderBy(l => l.SortOrder).ToList();
        var accountsByRef = mappings
            .GroupBy(m => m.Value.RefCode)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToList());

        decimal SumNets(Dictionary<string, decimal> nets, string refCode)
            => accountsByRef.TryGetValue(refCode, out var codes)
                ? codes.Sum(c => nets.GetValueOrDefault(c))
                : 0m;

        var components = new List<EquityComponentDto>();
        foreach (var line in equityLines)
        {
            // ส่วนผู้ถือหุ้นยอดเครดิต → กลับเครื่องหมาย (flip) ให้เป็นบวก
            decimal opening = -SumNets(openingNets, line.RefCode);
            decimal closingFromGl = -SumNets(closingNets, line.RefCode);

            decimal npCol = line.RefCode == "RE" ? netProfit : 0m;
            // RE ในงบดุลคำนวณเป็น (flip closing) + netProfit → ใช้สูตรเดียวกันให้ตรง
            decimal closing = line.RefCode == "RE" ? closingFromGl + netProfit : closingFromGl;
            decimal other = closing - opening - npCol;   // เพิ่มทุน/เงินปันผล/ปรับปรุง (ส่วนที่เหลือ)

            components.Add(new EquityComponentDto(
                line.RefCode, line.LineName,
                Math.Round(opening, 2), Math.Round(npCol, 2), Math.Round(other, 2), Math.Round(closing, 2)));
        }

        decimal bsEquity = components.Sum(c => c.Closing);
        return new EquityChangesDto(client.Id, client.LegalName, request.FiscalYear, components, Math.Round(bsEquity, 2));
    }

    private static async Task<Dictionary<string, decimal>> GetAccountNetsAsync(
        IApplicationDbContext db, int clientCompanyId, DateTime toExclusive, CancellationToken ct)
    {
        var lines = await db.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntry.ClientCompanyId == clientCompanyId && l.JournalEntry.JournalDate < toExclusive)
            .Select(l => new { l.Account.AccountCode, l.DebitAmount, l.CreditAmount })
            .ToListAsync(ct);
        return lines.GroupBy(l => l.AccountCode)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.DebitAmount - l.CreditAmount));
    }
}
