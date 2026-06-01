using Datacenter.Domain.Entities;

namespace Datacenter.Application.Features.Import.Services;

/// <summary>
/// Pure stateless service — validates staged import rows and builds error detail records.
/// No DB access; all inputs are passed in, making this fully unit-testable.
/// </summary>
public static class ImportValidationService
{
    public static List<ImportBatchDetail> ValidateAndBuildDetails(
        int batchId,
        IReadOnlyList<StagingAccount> accounts,
        IReadOnlyList<StagingTrialBalance> tbRows)
    {
        var details = new List<ImportBatchDetail>();
        int row = 1;

        foreach (var acc in accounts)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(acc.AccountCode)) errors.Add("AccountCode ว่างเปล่า");
            if (string.IsNullOrWhiteSpace(acc.AccountName)) errors.Add("AccountName ว่างเปล่า");
            if (acc.Group is < 1 or > 5) errors.Add($"Group '{acc.Group}' ไม่ถูกต้อง (1-5)");

            bool isValid = errors.Count == 0;
            acc.IsValid = isValid;
            acc.ValidationError = isValid ? null : string.Join("; ", errors);

            if (!isValid)
                details.Add(new ImportBatchDetail
                {
                    ImportBatchId = batchId,
                    RowNumber     = row,
                    AccountCode   = acc.AccountCode,
                    IsValid       = false,
                    ErrorMessage  = acc.ValidationError,
                    RawData       = $"GLACC: {acc.AccountCode} | {acc.AccountName}",
                });
            row++;
        }

        foreach (var tb in tbRows)
        {
            decimal expected = tb.BeginBalance + tb.TotalDebit - tb.TotalCredit;
            if (tb.PeriodSet == "CUR")
                expected += tb.ClosingDebit - tb.ClosingCredit;

            bool balanceOk = Math.Abs(expected - tb.EndBalance) <= 0.01m;
            tb.IsValid = balanceOk;
            tb.ValidationError = balanceOk ? null
                : $"EndBalance ไม่ตรง: คำนวณได้ {expected:N2} แต่ไฟล์ระบุ {tb.EndBalance:N2}";

            if (!balanceOk)
                details.Add(new ImportBatchDetail
                {
                    ImportBatchId = batchId,
                    RowNumber     = row,
                    AccountCode   = tb.AccountCode,
                    IsValid       = false,
                    ErrorMessage  = tb.ValidationError,
                    RawData       = $"GLBAL/{tb.PeriodSet}: {tb.AccountCode}",
                });
            row++;
        }

        return details;
    }
}
