using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.FinancialStatement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.FinancialStatement.Queries;

/// <summary>
/// ตรวจบัญชี "ตกหล่น" — บัญชีที่มียอดสะสมถึงสิ้นปี (≠0) แต่ยังไม่ถูก map เข้าบรรทัดงบ (RefCode).
/// บัญชีเหล่านี้จะถูกตัดออกจากงบ → ทำให้งบดุลไม่สมดุล (ผลรวมยอด = ผลต่างที่เกิด).
/// ใช้เตือนก่อนปิดงบในหน้า "จัดการ Mapping".
/// </summary>
public record GetUnmappedAccountsQuery(int ClientCompanyId, int FiscalYear)
    : IRequest<UnmappedAccountsResultDto>, IRequireCompanyAccess;

public class GetUnmappedAccountsQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetUnmappedAccountsQuery, UnmappedAccountsResultDto>
{
    public async Task<UnmappedAccountsResultDto> Handle(GetUnmappedAccountsQuery request, CancellationToken ct)
    {
        await accessGuard.EnsureAccessAsync(request.ClientCompanyId, ct);

        _ = await db.ClientCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        var mapped = await db.AccountStatementMappings.AsNoTracking()
            .Where(m => m.ClientCompanyId == request.ClientCompanyId)
            .Select(m => m.AccountCode)
            .ToListAsync(ct);
        var mappedSet = mapped.ToHashSet();

        var accountNames = await db.Accounts.AsNoTracking()
            .Where(a => a.ClientCompanyId == request.ClientCompanyId)
            .ToDictionaryAsync(a => a.AccountCode, a => a.AccountName, ct);

        // ยอดสะสม (debit−credit) ถึงสิ้นปีงบ = OPEN-Y + MOVE-Y — ฐานเดียวกับงบ
        // (กัน OPEN-(Y+1) เบิ้ล — ดู FsJournalNets / fs-cumulative-double-count)
        var nets = await Services.FsJournalNets.CumulativeAsync(
            db, request.ClientCompanyId, request.FiscalYear, ct);

        var items = nets
            .Where(kv => !mappedSet.Contains(kv.Key) && Math.Abs(kv.Value) > 0.01m)
            .Select(kv => new UnmappedAccountDto(
                kv.Key,
                accountNames.GetValueOrDefault(kv.Key, ""),
                Math.Round(kv.Value, 2)))
            .OrderByDescending(x => Math.Abs(x.NetBalance))
            .ToList();

        return new UnmappedAccountsResultDto(
            request.FiscalYear,
            mappedSet.Count,
            items.Count,
            Math.Round(items.Sum(x => x.NetBalance), 2),
            items);
    }
}
