using System.Security.Cryptography;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Bank.DTOs;
using Datacenter.Application.Features.Bank.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Commands;

// ── preview (parse อย่างเดียว ไม่บันทึก) ─────────────────────────────────────────
public record ParseBankStatementPreviewCommand(int ClientCompanyId, string FileName, byte[] Content)
    : IRequest<BankStatementParsePreviewDto>, IRequireCompanyAccess;

public class ParseBankStatementPreviewCommandHandler(IBankStatementParser parser)
    : IRequestHandler<ParseBankStatementPreviewCommand, BankStatementParsePreviewDto>
{
    public Task<BankStatementParsePreviewDto> Handle(ParseBankStatementPreviewCommand request, CancellationToken ct)
    {
        if (request.Content is not { Length: > 0 }) throw new DomainException("ไฟล์ว่าง");
        var r = parser.Parse(request.Content, request.FileName);
        var dto = new BankStatementParsePreviewDto(
            r.BankCode, r.AccountNo, r.PeriodStart, r.PeriodEnd,
            r.OpeningBalance, r.ClosingBalance, r.ComputedClosing, r.BalanceCheckPasses, r.Warning,
            r.Lines.Select(l => new BankStatementParsePreviewLineDto(
                l.Date, l.Description, l.Withdrawal, l.Deposit, l.Balance)).ToList());
        return Task.FromResult(dto);
    }
}

// ── upload (parse + บันทึก + auto-match) ─────────────────────────────────────────
public record UploadBankStatementCommand(
    int ClientCompanyId, int BankAccountId, string FileName, byte[] Content,
    decimal? OpeningBalance, decimal? ClosingBalance, string? Note)
    : IRequest<int>, IRequireCompanyAccess;

public class UploadBankStatementCommandHandler(
    IApplicationDbContext db, IBankStatementParser parser, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<UploadBankStatementCommand, int>
{
    public async Task<int> Handle(UploadBankStatementCommand request, CancellationToken ct)
    {
        if (request.Content is not { Length: > 0 }) throw new DomainException("ไฟล์ว่าง");

        var account = await db.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BankAccountId
                                   && b.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankAccount", request.BankAccountId);

        var parsed = parser.Parse(request.Content, request.FileName);
        if (parsed.Lines.Count == 0)
            throw new DomainException(parsed.Warning ?? "ไม่พบรายการใน statement");

        var ps = parsed.PeriodStart ?? parsed.Lines.Min(l => l.Date);
        var pe = parsed.PeriodEnd ?? parsed.Lines.Max(l => l.Date);
        var sha = Convert.ToHexString(SHA256.HashData(request.Content)).ToLowerInvariant();

        var import = new BankStatementImport
        {
            ClientCompanyId = request.ClientCompanyId,
            BankAccountId = account.Id,
            BankCode = parsed.BankCode,
            StatementAccountNo = parsed.AccountNo,
            PeriodStart = ps,
            PeriodEnd = pe,
            OpeningBalance = request.OpeningBalance ?? parsed.OpeningBalance,
            ClosingBalance = request.ClosingBalance ?? parsed.ClosingBalance,
            ParsedOk = parsed.BalanceCheckPasses,
            SourceFileName = request.FileName,
            SourceContent = request.Content,
            Sha256 = sha,
            ByteSize = request.Content.LongLength,
            Note = request.Note,
            Status = BankStatementImportStatus.Draft,
            CreatedBy = currentUser.Username,
        };
        foreach (var l in parsed.Lines)
            import.Lines.Add(new BankStatementLine
            {
                LineDate = l.Date,
                Description = l.Description,
                Withdrawal = Math.Round(l.Withdrawal, 2),
                Deposit = Math.Round(l.Deposit, 2),
                Balance = l.Balance,
                MatchStatus = BankLineMatchStatus.Unmatched,
                CreatedBy = currentUser.Username,
            });

        db.BankStatementImports.Add(import);
        await db.SaveChangesAsync(ct); // ได้ Id ของ import + lines

        await AutoMatchAsync(db, import, account.BankAccountCode, ct);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync("ImportBankStatement", "BankStatementImport",
            entityId: import.Id.ToString(),
            afterValue: $"{parsed.BankCode} {ps:yyyy-MM-dd}..{pe:yyyy-MM-dd} lines={parsed.Lines.Count}",
            companyId: request.ClientCompanyId, cancellationToken: ct);

        return import.Id;
    }

    /// <summary>auto-match บรรทัด statement กับ BankTransaction ของบัญชีในช่วงงวด</summary>
    internal static async Task AutoMatchAsync(
        IApplicationDbContext db, BankStatementImport import, string bankAccountCode, CancellationToken ct)
    {
        var from = import.PeriodStart.Date.AddDays(-7);
        var to = import.PeriodEnd.Date.AddDays(7);
        var txns = await db.BankTransactions.AsNoTracking()
            .Where(t => t.ClientCompanyId == import.ClientCompanyId
                     && t.BankAccountCode == bankAccountCode
                     && t.TransactionDate >= from && t.TransactionDate <= to)
            .Select(t => new BankReconciliationMatcher.BookTxn(t.Id, t.TransactionDate, t.IsDeposit, t.Amount))
            .ToListAsync(ct);

        var stmt = import.Lines.Select(l =>
            new BankReconciliationMatcher.StmtLine(l.Id, l.LineDate, l.Deposit, l.Withdrawal));

        var map = BankReconciliationMatcher.Match(stmt, txns);
        foreach (var line in import.Lines)
            if (map.TryGetValue(line.Id, out var bid))
            {
                line.MatchedBankTransactionId = bid;
                line.MatchStatus = BankLineMatchStatus.AutoMatched;
            }
    }
}
