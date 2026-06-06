using Datacenter.Application.Features.Prepaid.DTOs;
using Datacenter.Application.Features.Prepaid.Services;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Prepaid;

internal static class PrepaidMapper
{
    public static void Apply(PrepaidExpense e, PrepaidExpenseInput d)
    {
        e.Code = string.IsNullOrWhiteSpace(d.Code) ? null : d.Code.Trim();
        e.Name = d.Name.Trim();
        e.Reference = string.IsNullOrWhiteSpace(d.Reference) ? null : d.Reference.Trim();
        e.TotalAmount = d.TotalAmount;
        e.StartDate = d.StartDate.Date;
        e.EndDate = d.EndDate.Date;
        e.PrepaidAccountId = d.PrepaidAccountId;
        e.ExpenseAccountId = d.ExpenseAccountId;
        e.Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim();
        e.AttachmentPath = string.IsNullOrWhiteSpace(d.AttachmentPath) ? null : d.AttachmentPath.Trim();
        e.IsActive = d.IsActive;
    }

    public static PrepaidListItemDto ToListItem(PrepaidExpense e)
        => new(e.Id, e.Code, e.Name, e.Reference, e.TotalAmount, e.StartDate, e.EndDate, e.IsActive);

    public static PrepaidExpenseDto ToDto(PrepaidExpense e, IReadOnlyDictionary<int, Account> accounts)
    {
        string? Code(int id) => accounts.TryGetValue(id, out var a) ? a.AccountCode : null;
        return new PrepaidExpenseDto(
            e.Id, e.ClientCompanyId, e.Code, e.Name, e.Reference,
            e.TotalAmount, e.StartDate, e.EndDate,
            e.PrepaidAccountId, Code(e.PrepaidAccountId),
            e.ExpenseAccountId, Code(e.ExpenseAccountId),
            e.Notes, e.AttachmentPath, e.IsActive,
            PrepaidAmortizationEngine.TotalDays(e));
    }
}
