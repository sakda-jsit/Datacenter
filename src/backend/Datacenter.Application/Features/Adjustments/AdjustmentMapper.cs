using Datacenter.Application.Features.Adjustments.DTOs;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Adjustments;

internal static class AdjustmentMapper
{
    /// <summary>map entity → DTO. ต้องมี acc lookup (AccountId → Account) เพื่อเติมรหัส/ชื่อบัญชี</summary>
    public static AdjustmentEntryDto ToDto(AdjustmentEntry e, IReadOnlyDictionary<int, Account> accounts)
        => new(
            e.Id,
            e.ClientCompanyId,
            e.FiscalYear,
            e.DocumentNo,
            e.EntryDate,
            e.SourceType,
            e.Reference,
            e.Reason,
            e.AttachmentPath,
            e.CreatedBy,
            e.CreatedAt,
            e.Lines
                .OrderBy(l => l.Id)
                .Select(l => new AdjustmentLineDto(
                    l.Id,
                    l.AccountId,
                    accounts.TryGetValue(l.AccountId, out var a) ? a.AccountCode : string.Empty,
                    accounts.TryGetValue(l.AccountId, out var a2) ? a2.AccountName : string.Empty,
                    l.DebitAmount,
                    l.CreditAmount,
                    l.Description))
                .ToList());
}
