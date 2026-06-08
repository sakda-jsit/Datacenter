using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Enums;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Commands;

// ── จับคู่เอง ─────────────────────────────────────────────────────────────────
public record MatchBankLineCommand(int ClientCompanyId, int ImportId, int StatementLineId, int BankTransactionId)
    : IRequest, IRequireCompanyAccess;

public class MatchBankLineCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<MatchBankLineCommand>
{
    public async Task Handle(MatchBankLineCommand request, CancellationToken ct)
    {
        var line = await db.BankStatementLines
            .FirstOrDefaultAsync(l => l.Id == request.StatementLineId
                && l.BankStatementImportId == request.ImportId
                && l.Import.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankStatementLine", request.StatementLineId);

        var txn = await db.BankTransactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.BankTransactionId
                && t.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankTransaction", request.BankTransactionId);

        bool lineIsDeposit = line.Deposit > 0;
        if (txn.IsDeposit != lineIsDeposit)
            throw new DomainException("ทิศทาง (ฝาก/ถอน) ไม่ตรงกัน จับคู่ไม่ได้");

        // ปลด txn นี้จากบรรทัดอื่นในรอบเดียวกันก่อน (1:1)
        var others = await db.BankStatementLines
            .Where(l => l.BankStatementImportId == request.ImportId
                     && l.MatchedBankTransactionId == request.BankTransactionId
                     && l.Id != line.Id)
            .ToListAsync(ct);
        foreach (var o in others) { o.MatchedBankTransactionId = null; o.MatchStatus = BankLineMatchStatus.Unmatched; }

        line.MatchedBankTransactionId = txn.Id;
        line.MatchStatus = BankLineMatchStatus.ManualMatched;
        line.ModifiedBy = currentUser.Username;
        line.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── ปลดคู่ ────────────────────────────────────────────────────────────────────
public record UnmatchBankLineCommand(int ClientCompanyId, int ImportId, int StatementLineId)
    : IRequest, IRequireCompanyAccess;

public class UnmatchBankLineCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UnmatchBankLineCommand>
{
    public async Task Handle(UnmatchBankLineCommand request, CancellationToken ct)
    {
        var line = await db.BankStatementLines
            .FirstOrDefaultAsync(l => l.Id == request.StatementLineId
                && l.BankStatementImportId == request.ImportId
                && l.Import.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankStatementLine", request.StatementLineId);

        line.MatchedBankTransactionId = null;
        line.MatchStatus = BankLineMatchStatus.Unmatched;
        line.ModifiedBy = currentUser.Username;
        line.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── ลบรอบนำเข้า (ทุกคนลบได้ + audit ตาม req #7) ─────────────────────────────────
public record DeleteBankStatementImportCommand(int ClientCompanyId, int ImportId)
    : IRequest, IRequireCompanyAccess;

public class DeleteBankStatementImportCommandHandler(IApplicationDbContext db, IAuditService audit)
    : IRequestHandler<DeleteBankStatementImportCommand>
{
    public async Task Handle(DeleteBankStatementImportCommand request, CancellationToken ct)
    {
        var import = await db.BankStatementImports
            .FirstOrDefaultAsync(i => i.Id == request.ImportId && i.ClientCompanyId == request.ClientCompanyId, ct)
            ?? throw new NotFoundException("BankStatementImport", request.ImportId);

        db.BankStatementImports.Remove(import); // lines cascade
        await audit.LogAsync("DeleteBankStatement", "BankStatementImport",
            entityId: import.Id.ToString(), beforeValue: $"{import.BankCode} {import.PeriodStart:yyyy-MM-dd}",
            companyId: request.ClientCompanyId, cancellationToken: ct);
        await db.SaveChangesAsync(ct);
    }
}
