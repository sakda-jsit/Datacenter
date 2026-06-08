using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Bank.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Queries;

/// <summary>รายงานกระทบยอด statement กับสมุด (RPT-015/016): matched / unmatched 2 ฝั่ง + ตรวจยอดปลาย</summary>
public record GetBankReconciliationQuery(int ClientCompanyId, int ImportId)
    : IRequest<BankReconciliationDto>, IRequireCompanyAccess;

public class GetBankReconciliationQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBankReconciliationQuery, BankReconciliationDto>
{
    public async Task<BankReconciliationDto> Handle(GetBankReconciliationQuery request, CancellationToken ct)
    {
        var import = await db.BankStatementImports.AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.ImportId && i.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankStatementImport", request.ImportId);

        var account = await db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == import.BankAccountId, ct)
            ?? throw new NotFoundException("BankAccount", import.BankAccountId);

        var clientName = await db.ClientCompanies.AsNoTracking()
            .Where(c => c.Id == request.ClientCompanyId).Select(c => c.LegalName).FirstOrDefaultAsync(ct) ?? "";

        // รายการในสมุดของบัญชีนี้ (ทั้งหมด ≤ ปลายงวด เพื่อคำนวณยอดสมุด, + ในงวดเพื่อหา unmatched)
        var allTxns = await db.BankTransactions.AsNoTracking()
            .Where(t => t.ClientCompanyId == request.ClientCompanyId && t.BankAccountCode == account.BankAccountCode)
            .ToListAsync(ct);

        decimal bookClosing = Math.Round(account.BalanceForward
            + allTxns.Where(t => t.TransactionDate.Date <= import.PeriodEnd.Date)
                     .Sum(t => t.IsDeposit ? t.Amount : -t.Amount), 2);

        var txnById = allTxns.ToDictionary(t => t.Id);
        var matchedTxnIds = import.Lines.Where(l => l.MatchedBankTransactionId is not null)
            .Select(l => l.MatchedBankTransactionId!.Value).ToHashSet();

        var matched = new List<ReconMatchedPairDto>();
        var unmatchedStmt = new List<ReconStatementLineDto>();
        foreach (var l in import.Lines.OrderBy(l => l.LineDate).ThenBy(l => l.Id))
        {
            if (l.MatchedBankTransactionId is int bid && txnById.TryGetValue(bid, out var bt))
            {
                bool isDep = l.Deposit > 0;
                matched.Add(new ReconMatchedPairDto(l.Id, l.LineDate, l.Description,
                    isDep ? l.Deposit : l.Withdrawal, isDep, bt.Id, bt.TransactionDate, bt.CounterpartyName));
            }
            else
            {
                unmatchedStmt.Add(new ReconStatementLineDto(l.Id, l.LineDate, l.Description, l.Withdrawal, l.Deposit, l.Balance));
            }
        }

        var unmatchedBook = allTxns
            .Where(t => t.TransactionDate.Date >= import.PeriodStart.Date
                     && t.TransactionDate.Date <= import.PeriodEnd.Date
                     && !matchedTxnIds.Contains(t.Id))
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
            .Select(t => new ReconBookTxnDto(t.Id, t.TransactionDate, t.CounterpartyName, t.Remark,
                t.IsDeposit ? t.Amount : 0m, t.IsDeposit ? 0m : t.Amount))
            .ToList();

        // เอกลักษณ์กระทบยอด: statementClosing + unmatchedBookNet = bookClosing + unmatchedStmtNet
        decimal unmatchedBookNet = unmatchedBook.Sum(b => b.Deposit - b.Withdrawal);
        decimal unmatchedStmtNet = unmatchedStmt.Sum(s => s.Deposit - s.Withdrawal);
        decimal diff = Math.Round((import.ClosingBalance + unmatchedBookNet) - (bookClosing + unmatchedStmtNet), 2);

        DateTime? dataAsOf = allTxns.Count > 0 ? allTxns.Max(t => t.CreatedAt) : null;

        return new BankReconciliationDto(
            import.Id, request.ClientCompanyId, clientName,
            account.Id, account.BankAccountCode, account.BankName, import.BankCode,
            import.PeriodStart, import.PeriodEnd,
            import.OpeningBalance, import.ClosingBalance, bookClosing,
            diff, Math.Abs(diff) < 0.01m, import.ParsedOk,
            matched, unmatchedStmt, unmatchedBook, dataAsOf);
    }
}
