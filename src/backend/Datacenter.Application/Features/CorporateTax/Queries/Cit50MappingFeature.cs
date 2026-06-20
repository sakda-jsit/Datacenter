using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
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

// ScheduleNo: 4=ต้นทุนผลิต, 5=รายได้อื่น, 6=รายจ่ายอื่น, 8=ขายและบริหาร (default).
public record GetCit50MappingQuery(int ClientCompanyId, int FiscalYear, int ScheduleNo = 8)
    : IRequest<Cit50MappingViewDto>, IRequireCompanyAccess;

public class GetCit50MappingQueryHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GetCit50MappingQuery, Cit50MappingViewDto>
{
    internal static string ScopePrefix(int scheduleNo) => scheduleNo switch
    { 4 => "R4_", 5 => "R5_", 6 => "R6_", _ => "R8" };

    public async Task<Cit50MappingViewDto> Handle(GetCit50MappingQuery req, CancellationToken ct)
    {
        var prefix = ScopePrefix(req.ScheduleNo);
        var lines = (await db.Cit50ScheduleLines.AsNoTracking()
                .Where(l => l.ScheduleNo == req.ScheduleNo).OrderBy(l => l.SortOrder).ToListAsync(ct))
            .Where(l => !l.IsTotal) // บรรทัดคำนวณ (รวม/ซื้อ/หักคงเหลือ) ไม่รับ map
            .Select(l => new Cit50LineDto(l.Code, l.ScheduleNo, l.Label, l.IsCatchAll, l.IsTotal)).ToList();

        var maps = await db.AccountCit50Mappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == req.ClientCompanyId)
            .ToDictionaryAsync(m => m.AccountCode, m => m.Cit50LineCode, ct);

        // แสดงเฉพาะบัญชีที่ยังไม่ map หรือ map อยู่ใน scope นี้ (กันสับสน/กันลบข้ามแท็บ)
        bool InScope(string acc) => !maps.TryGetValue(acc, out var c) || c.StartsWith(prefix);

        var accounts = new List<Cit50AccountRowDto>();
        try
        {
            var pl = await sender.Send(new GetProfitLossQuery(req.ClientCompanyId, req.FiscalYear), ct);
            // กลุ่มบัญชีตามรายการ
            IEnumerable<FsLineAccountDto> pool = req.ScheduleNo switch
            {
                4 => pl.CostOfGoods.Accounts.Concat(pl.ExpenseLines.SelectMany(l => l.Accounts)),
                5 => pl.IncomeLines.Where(l => l.RefCode is not ("I1" or "I2")).SelectMany(l => l.Accounts),
                6 => pl.FinanceCost.Accounts.Concat(pl.ExpenseLines.SelectMany(l => l.Accounts)),
                _ => pl.ExpenseLines.SelectMany(l => l.Accounts),
            };
            foreach (var a in pool.GroupBy(a => a.AccountCode).Select(g => g.First()))
                if (InScope(a.AccountCode))
                    accounts.Add(new Cit50AccountRowDto(a.AccountCode, a.AccountName,
                        Math.Abs(a.NetBalance), maps.GetValueOrDefault(a.AccountCode)));

            // รายการ 4: เพิ่มบัญชีคงเหลือ (วัตถุดิบ/สินค้า A3) สำหรับ map ยอดต้น/ปลายงวด
            if (req.ScheduleNo == 4)
            {
                var invCodes = await db.AccountStatementMappings.AsNoTracking()
                    .Where(m => m.ClientCompanyId == req.ClientCompanyId && m.RefCode == "A3")
                    .Select(m => m.AccountCode).ToListAsync(ct);
                if (invCodes.Count > 0)
                {
                    var names = await db.Accounts.AsNoTracking()
                        .Where(x => x.ClientCompanyId == req.ClientCompanyId)
                        .ToDictionaryAsync(x => x.AccountCode, x => x.AccountName, ct);
                    var closes = await FinancialStatement.Services.FsJournalNets.CumulativeAsync(
                        db, req.ClientCompanyId, req.FiscalYear, ct);
                    foreach (var ac in invCodes.Where(InScope).Where(a => accounts.All(x => x.AccountCode != a)))
                        accounts.Add(new Cit50AccountRowDto(ac, names.GetValueOrDefault(ac, ""),
                            Math.Round(closes.GetValueOrDefault(ac), 2), maps.GetValueOrDefault(ac)));
                }
            }
        }
        catch { /* ไม่มีงบ → ไม่มีบัญชีให้แมพ */ }

        return new Cit50MappingViewDto(lines,
            accounts.OrderByDescending(a => Math.Abs(a.Amount)).ToList());
    }
}

// ── แมพบัญชี → บรรทัดงบดุล ภ.ง.ด.50 (รายการที่ 9, ScheduleNo=9) — override การจัดประเภท ──
// ใช้ตาราง AccountCit50Mappings ร่วมกับ schedule (คนละบัญชี: งบดุล=สินทรัพย์/หนี้สิน, schedule=ค่าใช้จ่าย).
// บันทึกผ่าน SaveCit50MappingCommand เดิมได้เลย.
public record GetCit50BsMappingQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<Cit50MappingViewDto>, IRequireCompanyAccess;

public class GetCit50BsMappingQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCit50BsMappingQuery, Cit50MappingViewDto>
{
    public async Task<Cit50MappingViewDto> Handle(GetCit50BsMappingQuery req, CancellationToken ct)
    {
        var lines = (await db.Cit50ScheduleLines.AsNoTracking()
                .Where(l => l.ScheduleNo == 9).OrderBy(l => l.SortOrder).ToListAsync(ct))
            .Select(l => new Cit50LineDto(l.Code, l.ScheduleNo, l.Label, l.IsCatchAll, l.IsTotal)).ToList();

        var allMaps = await db.AccountCit50Mappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == req.ClientCompanyId)
            .ToDictionaryAsync(m => m.AccountCode, m => m.Cit50LineCode, ct);
        // current = เฉพาะ BS_; บัญชีที่ map scope อื่น (R4_/R8_…) ซ่อนไว้ กันลบข้ามแท็บ
        var maps = allMaps.Where(kv => kv.Value.StartsWith("BS_")).ToDictionary(kv => kv.Key, kv => kv.Value);
        bool InScope(string acc) => !allMaps.TryGetValue(acc, out var c) || c.StartsWith("BS_");

        // บัญชีงบดุล (สินทรัพย์/หนี้สิน) ที่ map RefCode ไว้ + ยอดสะสมปลายปี (OPEN-Y + MOVE-Y)
        var refByAcc = await db.AccountStatementMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == req.ClientCompanyId)
            .ToDictionaryAsync(m => m.AccountCode, m => m.RefCode, ct);
        var names = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == req.ClientCompanyId)
            .ToDictionaryAsync(a => a.AccountCode, a => a.AccountName, ct);
        var nets = await FinancialStatement.Services.FsJournalNets.CumulativeAsync(
            db, req.ClientCompanyId, req.FiscalYear, ct);

        // แสดงเฉพาะบัญชีที่จัดประเภทต่อได้ (RefCode สินทรัพย์/หนี้สินใน Pnd50BsLines) — ยอด != 0
        var accounts = refByAcc
            .Where(kv => Pnd50BsLines.FieldByRefCode.ContainsKey(kv.Value)
                      && Math.Abs(nets.GetValueOrDefault(kv.Key)) > 0.005m
                      && InScope(kv.Key))
            .Select(kv =>
            {
                var isAsset = Pnd50BsLines.IsAssetField(Pnd50BsLines.FieldByRefCode[kv.Value]);
                var net = nets.GetValueOrDefault(kv.Key);
                return new Cit50AccountRowDto(kv.Key, names.GetValueOrDefault(kv.Key, ""),
                    Math.Round(isAsset ? net : -net, 2), maps.GetValueOrDefault(kv.Key));
            })
            .OrderByDescending(a => Math.Abs(a.Amount))
            .ToList();

        return new Cit50MappingViewDto(lines, accounts);
    }
}

// Scope = prefix ของ scope ที่กำลังบันทึก (เช่น "R4_","R8","BS_") — ล้าง mapping เฉพาะที่อยู่ใน scope นี้
// เท่านั้น กันลบ mapping ของแท็บอื่นเมื่อบัญชีเดียวกันโผล่หลายแท็บ. null = ล้างได้ทุกอัน (back-compat).
public record SaveCit50MappingCommand(int ClientCompanyId, IReadOnlyList<Cit50MappingItemInput> Items, string? Scope = null)
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
                // ล้างเฉพาะ mapping ที่อยู่ใน scope ที่กำลังบันทึก (กันลบของแท็บอื่น)
                if (has && (req.Scope is null || row!.Cit50LineCode.StartsWith(req.Scope)))
                    db.AccountCit50Mappings.Remove(row!);
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
