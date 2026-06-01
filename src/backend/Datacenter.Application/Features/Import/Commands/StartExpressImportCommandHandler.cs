using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Import.Services;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Commands;

public class StartExpressImportCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IExpressDbfAdapter dbfAdapter,
    IImportStorageService storage,
    IAuditService audit)
    : IRequestHandler<StartExpressImportCommand, int>
{
    public async Task<int> Handle(StartExpressImportCommand request, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ClientCompanyId && x.IsActive, ct)
            ?? throw new NotFoundException("ClientCompany", request.ClientCompanyId);

        if (!storage.ExpressFolderExists(client.Code))
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", $"ไม่พบโฟลเดอร์ Express DBF สำหรับรหัส '{client.Code}' กรุณาตรวจสอบการตั้งค่า Import:ExpressBasePath") });

        var folderPath = storage.GetExpressFolderPath(client.Code);

        if (!await dbfAdapter.FolderIsValidAsync(folderPath, ct))
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", $"ไม่พบไฟล์ ISINFO.DBF ในโฟลเดอร์ '{folderPath}'") });

        var batch = new ImportBatch
        {
            ClientCompanyId = request.ClientCompanyId,
            SourceType      = ImportSourceType.ExpressDbf,
            ImportType      = "TrialBalance",
            FiscalYear      = request.FiscalYear,
            Status          = ImportStatus.Running,
            CreatedBy       = currentUser.Username,
        };
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        // บันทึก audit ทันทีหลังสร้าง batch เพื่อให้ตรวจสอบย้อนกลับได้แม้การประมวลผลจะล้มเหลว
        await audit.LogAsync(
            action: "StartImport",
            entityName: "ImportBatch",
            entityId: batch.Id.ToString(),
            afterValue: $"{client.Code}/{request.FiscalYear}",
            companyId: request.ClientCompanyId,
            cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        try
        {
            var accounts        = await dbfAdapter.ReadAccountsAsync(folderPath, ct);
            var trialBalanceRows = await dbfAdapter.ReadTrialBalanceAsync(folderPath, ct);

            var stagingAccounts = accounts.Select(a => new StagingAccount
            {
                ImportBatchId = batch.Id,
                ClientCompanyId = request.ClientCompanyId,
                AccountCode  = a.AccountCode,
                AccountName  = a.AccountName,
                AccountName2 = a.AccountName2,
                Level        = a.Level,
                ParentCode   = a.ParentCode,
                Group        = a.Group,
                AccountType  = a.AccountType,
            }).ToList();
            db.StagingAccounts.AddRange(stagingAccounts);

            var stagingTb = trialBalanceRows.Select(r => new StagingTrialBalance
            {
                ImportBatchId   = batch.Id,
                ClientCompanyId = request.ClientCompanyId,
                AccountCode     = r.AccountCode,
                PeriodSet       = r.PeriodSet,
                FiscalYear      = request.FiscalYear,
                BeginBalance    = r.BeginBalance,
                TotalDebit      = r.TotalDebit,
                TotalCredit     = r.TotalCredit,
                ClosingDebit    = r.ClosingDebit,
                ClosingCredit   = r.ClosingCredit,
                EndBalance      = r.EndBalance,
            }).ToList();
            db.StagingTrialBalances.AddRange(stagingTb);

            var details = ImportValidationService.ValidateAndBuildDetails(batch.Id, stagingAccounts, stagingTb);
            db.ImportBatchDetails.AddRange(details);

            int errorCount      = details.Count(d => !d.IsValid);
            batch.TotalRows     = stagingAccounts.Count + stagingTb.Count;
            batch.SuccessRows   = batch.TotalRows - errorCount;
            batch.ErrorRows     = errorCount;
            batch.Status        = errorCount == 0 ? ImportStatus.Success : ImportStatus.Failed;
            batch.FinishedAt    = DateTime.UtcNow;
            batch.Message       = errorCount == 0
                ? $"นำเข้าข้อมูลสำเร็จ: บัญชี {stagingAccounts.Count} รายการ, ยอดคงเหลือ {stagingTb.Count} รายการ"
                : $"พบข้อผิดพลาด {errorCount} รายการ กรุณาตรวจสอบผลการตรวจสอบ";

            await db.SaveChangesAsync(ct);
            return batch.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            batch.Status     = ImportStatus.Failed;
            batch.FinishedAt = DateTime.UtcNow;
            batch.Message    = $"เกิดข้อผิดพลาด: {ex.Message}";
            // CancellationToken.None is intentional: the original token may already be cancelled,
            // but we must still persist the failure status so the batch record is not left as "Running".
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
