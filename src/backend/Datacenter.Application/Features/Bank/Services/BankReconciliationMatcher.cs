namespace Datacenter.Application.Features.Bank.Services;

/// <summary>
/// จับคู่บรรทัด statement กับรายการในสมุด (BankTransaction) แบบ greedy 1:1:
/// ทิศตรงกัน (ฝาก/ถอน) + จำนวนเท่ากัน (ปัด 2 ตำแหน่ง) + วันที่ห่าง ≤ tolerance (วันใกล้สุดก่อน).
/// pure — ไม่แตะ DB.
/// </summary>
public static class BankReconciliationMatcher
{
    public readonly record struct StmtLine(int Id, DateTime Date, decimal Deposit, decimal Withdrawal);
    public readonly record struct BookTxn(int Id, DateTime Date, bool IsDeposit, decimal Amount);

    /// <summary>คืน map: statementLineId → bankTransactionId (เฉพาะที่จับคู่ได้)</summary>
    public static Dictionary<int, int> Match(
        IEnumerable<StmtLine> statementLines, IEnumerable<BookTxn> bookTxns, int toleranceDays = 4)
    {
        var result = new Dictionary<int, int>();
        var usedBook = new HashSet<int>();
        var books = bookTxns.ToList();

        // จับคู่บรรทัดที่มีจำนวน > 0 เรียงตามวันที่
        foreach (var line in statementLines.OrderBy(l => l.Date))
        {
            bool lineIsDeposit = line.Deposit > 0;
            decimal amt = Math.Round(lineIsDeposit ? line.Deposit : line.Withdrawal, 2);
            if (amt <= 0) continue;

            int bestId = -1; int bestDiff = int.MaxValue;
            foreach (var b in books)
            {
                if (usedBook.Contains(b.Id)) continue;
                if (b.IsDeposit != lineIsDeposit) continue;
                if (Math.Round(b.Amount, 2) != amt) continue;
                int diff = Math.Abs((b.Date.Date - line.Date.Date).Days);
                if (diff > toleranceDays) continue;
                if (diff < bestDiff) { bestDiff = diff; bestId = b.Id; }
            }
            if (bestId >= 0) { result[line.Id] = bestId; usedBook.Add(bestId); }
        }
        return result;
    }
}
