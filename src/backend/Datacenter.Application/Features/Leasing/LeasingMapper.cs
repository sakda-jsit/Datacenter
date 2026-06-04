using Datacenter.Application.Features.Leasing.DTOs;
using Datacenter.Application.Features.Leasing.Services;
using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Leasing;

internal static class LeasingMapper
{
    /// <summary>คัดลอกค่าจาก input ลง entity (ใช้ทั้ง create/update)</summary>
    public static void Apply(LeaseContract e, LeaseContractInput d)
    {
        e.ContractType = d.ContractType;
        e.ContractNo = d.ContractNo.Trim();
        e.AssetName = d.AssetName.Trim();
        e.AssetCode = string.IsNullOrWhiteSpace(d.AssetCode) ? null : d.AssetCode.Trim();
        e.Lessor = string.IsNullOrWhiteSpace(d.Lessor) ? null : d.Lessor.Trim();
        e.ContractDate = d.ContractDate;
        e.FirstInstallmentDate = d.FirstInstallmentDate;
        e.NumberOfPeriods = d.NumberOfPeriods;
        e.PaymentsPerYear = d.PaymentsPerYear;
        e.CashPrice = d.CashPrice;
        e.DownPayment = d.DownPayment;
        e.FinancedPrincipal = d.FinancedPrincipal;
        e.InstallmentAmount = d.InstallmentAmount;
        e.VatPerPeriod = d.VatPerPeriod;
        e.LiabilityAccountId = d.LiabilityAccountId;
        e.DeferredInterestAccountId = d.DeferredInterestAccountId;
        e.InputVatUndueAccountId = d.InputVatUndueAccountId;
        e.InterestExpenseAccountId = d.InterestExpenseAccountId;
        e.Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes.Trim();
        e.AttachmentPath = string.IsNullOrWhiteSpace(d.AttachmentPath) ? null : d.AttachmentPath.Trim();
        e.IsActive = d.IsActive;
    }

    public static LeaseContractListItemDto ToListItem(LeaseContract e)
        => new(e.Id, e.ContractType, e.ContractNo, e.AssetName, e.AssetCode, e.Lessor,
            e.FirstInstallmentDate, e.NumberOfPeriods, e.FinancedPrincipal, e.InstallmentAmount, e.IsActive);

    public static LeaseContractDto ToDto(LeaseContract e, IReadOnlyDictionary<int, Account> accounts)
    {
        string? Code(int? id) => id.HasValue && accounts.TryGetValue(id.Value, out var a) ? a.AccountCode : null;

        var totalInterest = Math.Round(e.InstallmentAmount * e.NumberOfPeriods - e.FinancedPrincipal, 2);
        var totalVat = Math.Round(e.VatPerPeriod * e.NumberOfPeriods, 2);
        var grossTotal = Math.Round(e.FinancedPrincipal + totalInterest + totalVat, 2);
        var rate = LeaseAmortizationEngine.SolveRatePerPeriod(e.FinancedPrincipal, e.InstallmentAmount, e.NumberOfPeriods);

        return new LeaseContractDto(
            e.Id, e.ClientCompanyId, e.ContractType, e.ContractNo, e.AssetName, e.AssetCode, e.Lessor,
            e.ContractDate, e.FirstInstallmentDate, e.NumberOfPeriods, e.PaymentsPerYear,
            e.CashPrice, e.DownPayment, e.FinancedPrincipal, e.InstallmentAmount, e.VatPerPeriod,
            e.LiabilityAccountId, Code(e.LiabilityAccountId),
            e.DeferredInterestAccountId, Code(e.DeferredInterestAccountId),
            e.InputVatUndueAccountId, Code(e.InputVatUndueAccountId),
            e.InterestExpenseAccountId, Code(e.InterestExpenseAccountId),
            e.Notes, e.AttachmentPath, e.IsActive,
            totalInterest, totalVat, grossTotal, rate);
    }
}
