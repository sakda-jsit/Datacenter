using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Bank.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Queries;

/// <summary>รายชื่อบัญชีธนาคาร + ยอดคงเหลือปัจจุบัน (ยอดยกมา + เคลื่อนไหวสุทธิทั้งหมด)</summary>
public record GetBankAccountsQuery(int ClientCompanyId) : IRequest<IReadOnlyList<BankAccountDto>>, IRequireCompanyAccess;

public class GetBankAccountsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    public async Task<IReadOnlyList<BankAccountDto>> Handle(GetBankAccountsQuery request, CancellationToken ct)
    {
        var accounts = await db.BankAccounts
            .AsNoTracking()
            .Where(b => b.ClientCompanyId == request.ClientCompanyId)
            .OrderBy(b => b.BankAccountCode)
            .ToListAsync(ct);

        // เคลื่อนไหวสุทธิ + จำนวนรายการต่อบัญชี
        var movement = await db.BankTransactions
            .AsNoTracking()
            .Where(t => t.ClientCompanyId == request.ClientCompanyId)
            .GroupBy(t => t.BankAccountCode)
            .Select(g => new
            {
                Code = g.Key,
                Net = g.Sum(x => x.IsDeposit ? x.Amount : -x.Amount),
                Count = g.Count(),
            })
            .ToDictionaryAsync(x => x.Code, ct);

        return accounts.Select(b =>
        {
            movement.TryGetValue(b.BankAccountCode, out var m);
            return new BankAccountDto(
                b.Id, b.BankAccountCode, b.BankName, b.Branch, b.AccountNumber, b.GlAccountCode,
                b.BalanceForward, Math.Round(b.BalanceForward + (m?.Net ?? 0m), 2), m?.Count ?? 0);
        }).ToList();
    }
}
