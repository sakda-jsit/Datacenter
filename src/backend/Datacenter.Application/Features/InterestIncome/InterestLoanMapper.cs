using Datacenter.Application.Features.InterestIncome.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.InterestIncome;

internal static class InterestLoanMapper
{
    public static void Apply(InterestBearingLoan e, InterestLoanInput d)
    {
        e.Name = d.Name.Trim();
        e.Reference = string.IsNullOrWhiteSpace(d.Reference) ? null : d.Reference.Trim();
        e.AnnualRatePct = d.AnnualRatePct;
        e.SbtRatePct = d.SbtRatePct;
        e.LocalTaxPctOfSbt = d.LocalTaxPctOfSbt;
        e.DayCountBasis = d.DayCountBasis > 0 ? d.DayCountBasis : 365;
        e.InterestReceivableAccountId = d.InterestReceivableAccountId;
        e.InterestIncomeAccountId = d.InterestIncomeAccountId;
        e.Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim();
        e.AttachmentPath = string.IsNullOrWhiteSpace(d.AttachmentPath) ? null : d.AttachmentPath.Trim();
        e.IsActive = d.IsActive;

        e.Movements.Clear();
        var ordered = (d.Movements ?? [])
            .Where(m => m.Amount != 0m)
            .OrderBy(m => m.Date)
            .ToList();
        for (int i = 0; i < ordered.Count; i++)
            e.Movements.Add(new LoanPrincipalMovement
            {
                Date = ordered[i].Date.Date,
                Amount = ordered[i].Amount,
                Description = string.IsNullOrWhiteSpace(ordered[i].Description) ? null : ordered[i].Description!.Trim(),
                SortOrder = i,
            });
    }

    public static InterestLoanListItemDto ToListItem(InterestBearingLoan e)
        => new(e.Id, e.Name, e.Reference, e.AnnualRatePct, e.IsActive);

    public static InterestLoanDto ToDto(InterestBearingLoan e, IReadOnlyDictionary<int, Account> accounts)
    {
        string? Code(int id) => accounts.TryGetValue(id, out var a) ? a.AccountCode : null;
        var movements = e.Movements
            .OrderBy(m => m.Date)
            .Select(m => new LoanMovementDto(m.Date, m.Amount, m.Description))
            .ToList();
        return new InterestLoanDto(
            e.Id, e.ClientCompanyId, e.Name, e.Reference,
            e.AnnualRatePct, e.SbtRatePct, e.LocalTaxPctOfSbt, e.DayCountBasis,
            e.InterestReceivableAccountId, Code(e.InterestReceivableAccountId),
            e.InterestIncomeAccountId, Code(e.InterestIncomeAccountId),
            e.Notes, e.AttachmentPath, e.IsActive, movements);
    }
}
