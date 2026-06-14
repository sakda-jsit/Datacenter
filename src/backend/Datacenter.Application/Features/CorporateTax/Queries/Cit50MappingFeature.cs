using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.Queries;
using Datacenter.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.CorporateTax.Queries;

// แมพบัญชี → บรรทัด schedule CIT50 (รายการ 8 รายจ่ายขายและบริหาร) — ต่อบริษัท.

public record Cit50LineDto(string Code, int ScheduleNo, string Label, bool IsCatchAll, bool IsTotal);
public record Cit50AccountRowDto(string AccountCode, string AccountName, decimal Amount, string? Cit50LineCode);
public record Cit50MappingViewDto(
    IReadOnlyList<Cit50LineDto> Lines, IReadOnlyList<Cit50AccountRowDto> Accounts);
public record Cit50MappingItemInput(string AccountCode, string AccountName, string? Cit50LineCode);

public record GetCit50MappingQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<Cit50MappingViewDto>, IRequireCompanyAccess;

public class GetCit50MappingQueryHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetCit50MappingQuery, Cit50MappingViewDto>
{
    public async Task<Cit50MappingViewDto> Handle(GetCit50MappingQuery req, CancellationToken ct)
    {
        var lines = (await db.Cit50ScheduleLines.AsNoTracking()
                .Where(l => l.ScheduleNo == 8).OrderBy(l => l.SortOrder).ToListAsync(ct))
            .Select(l => new Cit50LineDto(l.Code, l.ScheduleNo, l.Label, l.IsCatchAll, l.IsTotal)).ToList();

        var maps = await db.AccountCit50Mappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == req.ClientCompanyId)
            .ToDictionaryAsync(m => m.AccountCode, m => m.Cit50LineCode, ct);

        // บัญชีรายจ่ายขายและบริหาร (X1+X2) + ต้นทุนการเงิน (X3) จากงบกำไรขาดทุน
        var accounts = new List<Cit50AccountRowDto>();
        try
        {
            var pl = await sender.Send(new GetProfitLossQuery(req.ClientCompanyId, req.FiscalYear), ct);
            foreach (var line in pl.ExpenseLines.Append(pl.FinanceCost))
                foreach (var a in line.Accounts)
                    accounts.Add(new Cit50AccountRowDto(a.AccountCode, a.AccountName,
                        Math.Abs(a.NetBalance), maps.GetValueOrDefault(a.AccountCode)));
        }
        catch { /* ไม่มีงบ → ไม่มีบัญชีให้แมพ */ }

        return new Cit50MappingViewDto(lines,
            accounts.OrderByDescending(a => a.Amount).ToList());
    }
}

public record SaveCit50MappingCommand(int ClientCompanyId, IReadOnlyList<Cit50MappingItemInput> Items)
    : IRequest, IRequireCompanyAccess;

public class SaveCit50MappingCommandHandler(IApplicationDbContext db, ICurrentUserService user, IAuditService audit)
    : IRequestHandler<SaveCit50MappingCommand>
{
    public async Task Handle(SaveCit50MappingCommand req, CancellationToken ct)
    {
        var existing = await db.AccountCit50Mappings
            .Where(m => m.ClientCompanyId == req.ClientCompanyId).ToListAsync(ct);
        var byAcc = existing.ToDictionary(m => m.AccountCode);

        foreach (var item in req.Items)
        {
            var has = byAcc.TryGetValue(item.AccountCode, out var row);
            if (string.IsNullOrWhiteSpace(item.Cit50LineCode))
            {
                if (has) db.AccountCit50Mappings.Remove(row!); // ล้างแมพ
                continue;
            }
            if (!has)
            {
                db.AccountCit50Mappings.Add(new AccountCit50Mapping
                {
                    ClientCompanyId = req.ClientCompanyId, AccountCode = item.AccountCode,
                    AccountName = item.AccountName, Cit50LineCode = item.Cit50LineCode.Trim(),
                    CreatedBy = user.Username,
                });
            }
            else
            {
                row!.Cit50LineCode = item.Cit50LineCode.Trim();
                row.AccountName = item.AccountName;
                row.ModifiedBy = user.Username; row.ModifiedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("Update", "AccountCit50Mapping", req.ClientCompanyId.ToString(),
            afterValue: $"แมพบัญชี→CIT50 {req.Items.Count(i => !string.IsNullOrWhiteSpace(i.Cit50LineCode))} บัญชี",
            companyId: req.ClientCompanyId, cancellationToken: ct);
    }
}
