using Datacenter.Application.Common.Interfaces;
using Datacenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Bank.Services;

/// <summary>
/// นำเข้าบัญชีธนาคาร (BKMAS, upsert master) + รายการเดินบัญชี (BKTRN, replace ต่อบริษัท)
/// จาก Express — เรียกใน pipeline นำเข้ากลาง (StartExpressImport). ไม่เรียก SaveChanges.
/// </summary>
public static class BankImporter
{
    public static async Task<(int Accounts, int Transactions, string Message)> ImportAsync(
        IApplicationDbContext db,
        IExpressDbfAdapter dbfAdapter,
        string folderPath,
        int clientCompanyId,
        int importBatchId,
        string username,
        CancellationToken ct)
    {
        var accounts = await dbfAdapter.ReadBankAccountsAsync(folderPath, ct);
        var txns = await dbfAdapter.ReadBankTransactionsAsync(folderPath, ct);
        if (accounts.Count == 0 && txns.Count == 0)
            return (0, 0, string.Empty);

        // ── บัญชีธนาคาร: upsert by (บริษัท, รหัส) ──
        var existing = await db.BankAccounts
            .Where(b => b.ClientCompanyId == clientCompanyId)
            .ToListAsync(ct);
        var byCode = existing.ToDictionary(b => b.BankAccountCode, StringComparer.Ordinal);

        foreach (var row in accounts)
        {
            if (!byCode.TryGetValue(row.BankAccountCode, out var acc))
            {
                acc = new BankAccount { ClientCompanyId = clientCompanyId, BankAccountCode = row.BankAccountCode, CreatedBy = username };
                db.BankAccounts.Add(acc);
            }
            else
            {
                acc.ModifiedBy = username;
                acc.ModifiedAt = DateTime.UtcNow;
            }
            acc.BankName = string.IsNullOrWhiteSpace(row.BankName) ? row.BankAccountCode : row.BankName;
            acc.Branch = Norm(row.Branch);
            acc.ShortName = Norm(row.ShortName);
            acc.AccountNumber = Norm(row.AccountNumber);
            acc.GlAccountCode = Norm(row.GlAccountCode);
            acc.BalanceForward = row.BalanceForward;
            acc.BalanceDate = row.BalanceDate;
            acc.IsActive = true;
        }

        // ── รายการเดินบัญชี: replace ทั้งชุดต่อบริษัท ──
        var oldTxns = await db.BankTransactions.Where(t => t.ClientCompanyId == clientCompanyId).ToListAsync(ct);
        if (oldTxns.Count > 0) db.BankTransactions.RemoveRange(oldTxns);

        foreach (var row in txns)
        {
            db.BankTransactions.Add(new BankTransaction
            {
                ClientCompanyId  = clientCompanyId,
                BankAccountCode  = row.BankAccountCode,
                TransactionDate  = row.TransactionDate,
                TransactionType  = Norm(row.TransactionType),
                IsDeposit        = row.IsDeposit,
                ChequeNo         = Norm(row.ChequeNo),
                ChequeDate       = row.ChequeDate,
                CounterpartyName = Norm(row.CounterpartyName),
                Amount           = row.Amount,
                Charge           = row.Charge,
                Remark           = Norm(row.Remark),
                Voucher          = Norm(row.Voucher),
                ChequeStatus     = Norm(row.ChequeStatus),
                ImportBatchId    = importBatchId,
                CreatedBy        = username,
            });
        }

        return (accounts.Count, txns.Count, $"บัญชีธนาคาร {accounts.Count} บัญชี, รายการเดินบัญชี {txns.Count} รายการ");
    }

    private static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
