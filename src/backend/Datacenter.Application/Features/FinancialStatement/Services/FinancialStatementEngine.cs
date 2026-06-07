using Datacenter.Application.Features.FinancialStatement.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.FinancialStatement.Services;

/// <summary>
/// Pure calculation engine — no DB access, no MediatR.
/// Converts raw trial-balance net amounts (debit - credit) into
/// presentation amounts using the sign convention from /reference/financial.
///
/// Sign convention:
///   Section A (Assets)   : keep natural sign  (debit balance = positive)
///   Section X (Expenses) : keep natural sign  (debit balance = positive)
///   Section L (Liabilities), E (Equity), I (Income) : flip sign
///     (credit balance stored as negative net → flip → display as positive)
///   X3, X4               : shown as negative deductions regardless of section
///
/// RE (Retained Earnings, RefCode="RE") special formula:
///   RE = -openingBalance + netProfit
///   where openingBalance = prior-year RE account net (credit = negative net → flip)
///   and   netProfit      = current year P&amp;L result
/// </summary>
public static class FinancialStatementEngine
{
    // Sections whose natural sign is kept (debit positive)
    private static readonly HashSet<char> DebitSections = ['A', 'X'];

    // REF codes displayed as deductions (always negative presentation)
    private static readonly HashSet<string> DeductionCodes = ["X3", "X4"];

    // ── Balance Sheet ──────────────────────────────────────────────────────────

    public static BalanceSheetDto BuildBalanceSheet(
        ClientCompany client,
        int fiscalYear,
        IReadOnlyList<StatementLine> allLines,
        // accountCode → (net = debit−credit for the full year ending)
        Dictionary<string, decimal> accountNetBalances,
        // accountCode → StatementLine mapping
        Dictionary<string, AccountStatementMapping> mappings,
        // accountCode → account entity (for names)
        Dictionary<string, Account> accounts,
        // prior-year net balance of the retained-earnings account (32000 pattern)
        decimal reOpeningNetBalance,
        // net profit calculated from P&L (passed in after P&L is computed)
        decimal netProfit,
        // income tax for the year (X4) — already deducted from netProfit above
        decimal incomeTax,
        // prepaid withholding tax applied against this year's income tax (WHT)
        decimal whtApplied)
    {
        var lineAmounts = AggregateByRefCode(accountNetBalances, mappings, accounts, allLines);

        // TXR/TXP are injected below from the income-tax settlement (not from mapped accounts),
        // so exclude them here to avoid an empty duplicate line.
        var assetLines = BuildLines(allLines, lineAmounts, 'A', exclude: ["TXR"]).ToList();
        var liabLines  = BuildLines(allLines, lineAmounts, 'L', exclude: ["TXP"]).ToList();
        var equityLines = BuildEquityLines(allLines, lineAmounts, reOpeningNetBalance, netProfit);

        // ── Income-tax settlement on the balance sheet (closes the X4 loop) ──────────
        // X4 reduced retained earnings via netProfit; book the counterpart here so the
        // statement balances:  DR tax expense / CR prepaid WHT (asset↓) + tax payable (liab↑).
        if (whtApplied != 0)
            ApplyToLine(assetLines, "A4", -whtApplied);   // consume prepaid WHT asset

        decimal netPayable = incomeTax - whtApplied;
        if (netPayable > 0.005m)
            liabLines.Add(BuildTaxLine(allLines, "TXP", "ภาษีเงินได้ค้างจ่าย", 'L', 35, netPayable));
        else if (netPayable < -0.005m)
            assetLines.Add(BuildTaxLine(allLines, "TXR", "ภาษีเงินได้จ่ายล่วงหน้า (รอขอคืน)", 'A', 17, -netPayable));

        assetLines = assetLines.OrderBy(l => l.SortOrder).ToList();
        liabLines  = liabLines.OrderBy(l => l.SortOrder).ToList();

        decimal totalAssets  = assetLines.Sum(l => l.Amount);
        decimal totalLiab    = liabLines.Sum(l => l.Amount);
        decimal totalEquity  = equityLines.Sum(l => l.Amount);
        decimal totalLE      = totalLiab + totalEquity;

        return new BalanceSheetDto(
            client.Id, client.Code, client.LegalName, fiscalYear,
            assetLines, liabLines, equityLines,
            totalAssets, totalLiab, totalEquity, totalLE,
            totalAssets - totalLE);
    }

    /// <summary>Adjusts an existing presentation line's amount in place (e.g. consume prepaid tax).</summary>
    private static void ApplyToLine(List<FsLineDto> lines, string refCode, decimal delta)
    {
        int i = lines.FindIndex(l => l.RefCode == refCode);
        if (i >= 0)
            lines[i] = lines[i] with { Amount = lines[i].Amount + delta };
    }

    /// <summary>Builds an injected tax line from its StatementLine definition, or a literal fallback.</summary>
    private static FsLineDto BuildTaxLine(
        IReadOnlyList<StatementLine> allLines,
        string refCode, string fallbackName, char fallbackSection, int fallbackSort, decimal amount)
    {
        var line = allLines.FirstOrDefault(l => l.RefCode == refCode);
        return new FsLineDto(
            refCode,
            line?.LineName ?? fallbackName,
            line?.Section ?? fallbackSection,
            line?.SortOrder ?? fallbackSort,
            amount, [], NotesEngine.NoteNoFor(refCode));
    }

    // ── Profit & Loss ──────────────────────────────────────────────────────────

    public static ProfitLossDto BuildProfitLoss(
        ClientCompany client,
        int fiscalYear,
        int? monthFrom,
        int? monthTo,
        IReadOnlyList<StatementLine> allLines,
        Dictionary<string, decimal> accountNetBalances,
        Dictionary<string, AccountStatementMapping> mappings,
        Dictionary<string, Account> accounts,
        decimal externalTax)  // X4 from FsExternalInput
    {
        var lineAmounts = AggregateByRefCode(accountNetBalances, mappings, accounts, allLines);

        // Income lines (section I) — flip sign
        var incomeLines = BuildLines(allLines, lineAmounts, 'I');
        decimal totalIncome = incomeLines.Sum(l => l.Amount);

        // COGS (C) — keep natural sign
        var cogLine = BuildSingleLine(allLines, lineAmounts, "C");
        decimal totalExpenses = cogLine.Amount;

        // Operating expenses (X1, X2) — keep natural sign.
        // Exclude "C" (cost of sales, reported separately above) and X3/X4 (finance cost & tax,
        // shown below as deductions) so none of them are double-counted in total expenses.
        var expLines = BuildLines(allLines, lineAmounts, 'X',
            exclude: ["C", "X3", "X4"]);
        totalExpenses += expLines.Sum(l => l.Amount);

        decimal grossProfit         = totalIncome - cogLine.Amount;
        decimal profitBeforeFinance = totalIncome - totalExpenses;

        // Finance cost X3 — shown as negative deduction
        var financeLine = BuildSingleLine(allLines, lineAmounts, "X3");
        decimal profitBeforeTax = profitBeforeFinance - Math.Abs(financeLine.Amount);

        // Tax X4 — from external input, shown as negative deduction
        var taxLine = BuildExternalLine(allLines, "X4", externalTax);
        decimal netProfit = profitBeforeTax - externalTax;

        return new ProfitLossDto(
            client.Id, client.Code, client.LegalName, fiscalYear, monthFrom, monthTo,
            incomeLines, cogLine, expLines, financeLine, taxLine,
            totalIncome, totalExpenses, grossProfit,
            profitBeforeFinance, profitBeforeTax, netProfit);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Groups trial-balance net amounts by RefCode.
    /// Returns: refCode → list of (accountCode, accountName, netBalance).
    /// </summary>
    private static Dictionary<string, List<FsLineAccountDto>> AggregateByRefCode(
        Dictionary<string, decimal> accountNets,
        Dictionary<string, AccountStatementMapping> mappings,
        Dictionary<string, Account> accounts,
        IReadOnlyList<StatementLine> allLines)
    {
        var result = allLines.ToDictionary(l => l.RefCode, _ => new List<FsLineAccountDto>());

        foreach (var (code, net) in accountNets)
        {
            if (!mappings.TryGetValue(code, out var mapping)) continue;
            var name = accounts.TryGetValue(code, out var acc)
                ? acc.AccountName
                : mapping.AccountName;
            if (!result.TryGetValue(mapping.RefCode, out var list)) continue;
            list.Add(new FsLineAccountDto(code, name, net));
        }

        return result;
    }

    private static IReadOnlyList<FsLineDto> BuildLines(
        IReadOnlyList<StatementLine> allLines,
        Dictionary<string, List<FsLineAccountDto>> lineAmounts,
        char section,
        HashSet<string>? exclude = null)
    {
        return allLines
            .Where(l => l.Section == section && (exclude == null || !exclude.Contains(l.RefCode)))
            .OrderBy(l => l.SortOrder)
            .Select(l => ToFsLine(l, lineAmounts))
            .ToList();
    }

    private static FsLineDto BuildSingleLine(
        IReadOnlyList<StatementLine> allLines,
        Dictionary<string, List<FsLineAccountDto>> lineAmounts,
        string refCode)
    {
        var line = allLines.First(l => l.RefCode == refCode);
        return ToFsLine(line, lineAmounts);
    }

    private static FsLineDto BuildExternalLine(
        IReadOnlyList<StatementLine> allLines,
        string refCode,
        decimal externalAmount)
    {
        var line = allLines.First(l => l.RefCode == refCode);
        // External amount already represents the expense (positive = expense)
        // Display as negative deduction
        decimal presentation = -Math.Abs(externalAmount);
        return new FsLineDto(
            line.RefCode, line.LineName, line.Section, line.SortOrder,
            presentation, [], NotesEngine.NoteNoFor(line.RefCode));
    }

    private static IReadOnlyList<FsLineDto> BuildEquityLines(
        IReadOnlyList<StatementLine> allLines,
        Dictionary<string, List<FsLineAccountDto>> lineAmounts,
        decimal reOpeningNetBalance,
        decimal netProfit)
    {
        return allLines
            .Where(l => l.Section == 'E')
            .OrderBy(l => l.SortOrder)
            .Select(l =>
            {
                if (l.RefCode == "RE")
                {
                    // RE = -openingNetBalance (flip credit→positive) + netProfit
                    // reOpeningNetBalance is (debit−credit) of RE account at year start
                    // RE accounts have credit balance → negative net → flip = positive
                    decimal reAmount = -reOpeningNetBalance + netProfit;
                    var accs = lineAmounts.TryGetValue("RE", out var list) ? list : [];
                    return new FsLineDto(
                        l.RefCode, l.LineName, l.Section, l.SortOrder, reAmount, accs,
                        NotesEngine.NoteNoFor(l.RefCode));
                }
                return ToFsLine(l, lineAmounts);
            })
            .ToList();
    }

    private static FsLineDto ToFsLine(
        StatementLine line,
        Dictionary<string, List<FsLineAccountDto>> lineAmounts)
    {
        var accs = lineAmounts.TryGetValue(line.RefCode, out var list) ? list : [];
        decimal rawSum = accs.Sum(a => a.NetBalance);  // sum of (debit−credit) per account

        decimal presentation;
        if (DeductionCodes.Contains(line.RefCode))
        {
            // X3, X4: always show as negative deduction
            presentation = -Math.Abs(rawSum);
        }
        else if (DebitSections.Contains(line.Section))
        {
            // Assets, Expenses: keep natural sign
            presentation = rawSum;
        }
        else
        {
            // Liabilities, Equity, Income: flip
            // Credit-balance accounts have negative net → flip → positive
            presentation = -rawSum;
        }

        return new FsLineDto(line.RefCode, line.LineName, line.Section, line.SortOrder,
            presentation, accs, NotesEngine.NoteNoFor(line.RefCode));
    }
}
