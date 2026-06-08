using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Bank.DTOs;
using Datacenter.Application.Features.Bank.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Queries;

/// <summary>รายการรอบนำเข้า statement ของบริษัท (กรองตามบัญชีได้)</summary>
public record GetBankStatementImportsQuery(int ClientCompanyId, int? BankAccountId)
    : IRequest<IReadOnlyList<BankStatementImportListDto>>, IRequireCompanyAccess;

public class GetBankStatementImportsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBankStatementImportsQuery, IReadOnlyList<BankStatementImportListDto>>
{
    public async Task<IReadOnlyList<BankStatementImportListDto>> Handle(GetBankStatementImportsQuery request, CancellationToken ct)
    {
        var q = db.BankStatementImports.AsNoTracking()
            .Where(i => i.ClientCompanyId == request.ClientCompanyId);
        if (request.BankAccountId is int bid) q = q.Where(i => i.BankAccountId == bid);

        var rows = await q.OrderByDescending(i => i.PeriodStart).ThenByDescending(i => i.Id)
            .Select(i => new
            {
                i.Id, i.BankAccountId, i.BankCode, i.StatementAccountNo,
                i.PeriodStart, i.PeriodEnd, i.OpeningBalance, i.ClosingBalance,
                i.ParsedOk, i.Status, LineCount = i.Lines.Count,
                MatchedCount = i.Lines.Count(l => l.MatchedBankTransactionId != null),
                i.CreatedAt, i.CreatedBy,
                ExpectedAccountNo = db.BankAccounts
                    .Where(b => b.Id == i.BankAccountId).Select(b => b.AccountNumber).FirstOrDefault(),
            })
            .ToListAsync(ct);

        return rows.Select(i => new BankStatementImportListDto(
                i.Id, i.BankAccountId, i.BankCode, i.StatementAccountNo,
                i.PeriodStart, i.PeriodEnd, i.OpeningBalance, i.ClosingBalance,
                i.ParsedOk, (int)i.Status, i.LineCount, i.MatchedCount,
                i.CreatedAt, i.CreatedBy,
                i.ExpectedAccountNo,
                BankAccountNumberComparer.Matches(i.StatementAccountNo, i.ExpectedAccountNo)))
            .ToList();
    }
}
