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

        // นิยามรอบบัญชี (ISPRD) เป็น source of truth — ปีที่นำเข้าต้องอยู่ในนิยามนี้เท่านั้น
        IReadOnlyList<Features.Import.DTOs.ExpressAccountingPeriodDto> periods;
        try
        {
            periods = await dbfAdapter.ReadAccountingPeriodsAsync(folderPath, ct);
        }
        catch (Exception ex)
        {
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", $"อ่านนิยามรอบบัญชี (ISPRD) ไม่สำเร็จ: {ex.Message}") });
        }

        var definedYears = periods.Select(p => p.EndDate.Year).Distinct().OrderBy(y => y).ToList();
        if (definedYears.Count == 0)
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "Path", "ไม่พบนิยามรอบบัญชีใน ISPRD") });

        if (!definedYears.Contains(request.FiscalYear))
            throw new Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure(
                    "FiscalYear", $"ปี {request.FiscalYear} ไม่อยู่ในรอบบัญชีของข้อมูลนี้ (รอบบัญชีที่กำหนด: {string.Join(", ", definedYears)})") });

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

            // แทนที่อัตโนมัติ: เมื่อนำเข้าสำเร็จ ลบ batch เดิมของบริษัท+ปีเดียวกันทิ้ง (1 บริษัท/ปี เหลือ batch เดียว)
            // ทำเฉพาะตอน Success เพื่อไม่ให้ข้อมูลเดิมหายหากนำเข้าใหม่ผิดพลาด
            if (batch.Status == ImportStatus.Success)
            {
                int replaced = await RemoveSupersededBatchesAsync(
                    request.ClientCompanyId, request.FiscalYear, batch.Id, ct);
                if (replaced > 0)
                    batch.Message += $" (แทนที่ข้อมูลเดิม {replaced} batch)";

                // บันทึกนิยามรอบบัญชี + seed สถานะปิดงวดตาม LOCK ของ Express
                await PersistAccountingPeriodsAsync(request.ClientCompanyId, periods, ct);
            }

            await db.SaveChangesAsync(ct);

            // Auto-post: เมื่อนำเข้าสำเร็จ ยกข้อมูลเข้า production ให้อัตโนมัติ เพื่อให้รายงานแสดงทันที
            // ห่อด้วย try/catch แยก เพื่อไม่ให้ความล้มเหลวของ post ทำให้ batch กลายเป็น Failed
            if (batch.Status == ImportStatus.Success)
            {
                try
                {
                    var postResult = await ExpressPostingService.PostAsync(db, batch, currentUser.Username, ct);
                    await audit.LogAsync(
                        action: "AutoPostImport",
                        entityName: "ImportBatch",
                        entityId: batch.Id.ToString(),
                        afterValue: $"auto-posted FY{request.FiscalYear}: opening={postResult.OpeningLines}, movement={postResult.MovementLines}",
                        companyId: request.ClientCompanyId,
                        cancellationToken: ct);
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    batch.IsPosted = false;
                    batch.Message += $" (post อัตโนมัติไม่สำเร็จ: {ex.Message} — กรุณากดปุ่ม Post เข้าบัญชี)";
                    await db.SaveChangesAsync(CancellationToken.None);
                }
            }

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

    /// <summary>
    /// ลบ batch เดิมของบริษัท+ปีเดียวกัน (ยกเว้น batch ปัจจุบัน) พร้อมข้อมูลที่เกี่ยวข้องทั้งหมด:
    /// posted JournalEntry (lines ลบตาม cascade), staging accounts/TB และ validation details
    /// คืนจำนวน batch ที่ถูกแทนที่ ยังไม่เรียก SaveChanges (รวมไว้กับการบันทึก batch ใหม่)
    /// </summary>
    private async Task<int> RemoveSupersededBatchesAsync(
        int companyId, int fiscalYear, int keepBatchId, CancellationToken ct)
    {
        var oldBatches = await db.ImportBatches
            .Where(b => b.ClientCompanyId == companyId && b.FiscalYear == fiscalYear && b.Id != keepBatchId)
            .ToListAsync(ct);
        if (oldBatches.Count == 0) return 0;

        var oldIds = oldBatches.Select(b => b.Id).ToList();

        var postedEntries = await db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.ImportBatchId != null && oldIds.Contains(j.ImportBatchId.Value))
            .ToListAsync(ct);
        if (postedEntries.Count > 0) db.JournalEntries.RemoveRange(postedEntries);

        var details = await db.ImportBatchDetails.Where(d => oldIds.Contains(d.ImportBatchId)).ToListAsync(ct);
        if (details.Count > 0) db.ImportBatchDetails.RemoveRange(details);

        var stagingAccounts = await db.StagingAccounts.Where(s => oldIds.Contains(s.ImportBatchId)).ToListAsync(ct);
        if (stagingAccounts.Count > 0) db.StagingAccounts.RemoveRange(stagingAccounts);

        var stagingTb = await db.StagingTrialBalances.Where(s => oldIds.Contains(s.ImportBatchId)).ToListAsync(ct);
        if (stagingTb.Count > 0) db.StagingTrialBalances.RemoveRange(stagingTb);

        db.ImportBatches.RemoveRange(oldBatches);
        return oldBatches.Count;
    }

    /// <summary>
    /// บันทึก/แทนที่นิยามรอบบัญชีของบริษัทตามปีที่ ISPRD ระบุ และ seed สถานะปิดงวด:
    /// งวดที่ Express ระบุ LOCK='Y' จะตั้ง ClosingPeriod เป็น Closed (ถ้ายังเปิดอยู่)
    /// ยังไม่เรียก SaveChanges (รวมไว้กับการบันทึก batch)
    /// </summary>
    private async Task PersistAccountingPeriodsAsync(
        int companyId,
        IReadOnlyList<Features.Import.DTOs.ExpressAccountingPeriodDto> periods,
        CancellationToken ct)
    {
        if (periods.Count == 0) return;

        var years = periods.Select(p => p.EndDate.Year).Distinct().ToList();

        // นิยามรอบบัญชีต้อง mirror ISPRD ปัจจุบันเสมอ → แทนที่ทั้งหมดของบริษัท
        // (ปีที่หลุดออกจากรอบบัญชีปัจจุบันจะไม่มีนิยาม = ถูกป้องกันไม่ให้ลบ)
        var existingPeriods = await db.AccountingPeriods
            .Where(p => p.ClientCompanyId == companyId)
            .ToListAsync(ct);
        if (existingPeriods.Count > 0)
            db.AccountingPeriods.RemoveRange(existingPeriods);

        // ClosingPeriod เดิมของปีเหล่านั้น (ไว้ upsert สถานะตาม LOCK)
        var closingRows = await db.ClosingPeriods
            .Where(c => c.ClientCompanyId == companyId && years.Contains(c.Year))
            .ToListAsync(ct);
        var closingByKey = closingRows.ToDictionary(c => (c.Year, c.Month));

        foreach (var p in periods)
        {
            int year = p.EndDate.Year;
            int periodNo = p.EndDate.Month; // ระบบใช้ Year+Month (1-12) — งวดอิงเดือนของวันสิ้นงวด

            db.AccountingPeriods.Add(new Domain.Entities.AccountingPeriod
            {
                ClientCompanyId = companyId,
                Year = year,
                PeriodNo = periodNo,
                BeginDate = p.BeginDate,
                EndDate = p.EndDate,
                SourceLocked = p.Locked,
                CreatedBy = currentUser.Username,
            });

            // seed: งวดที่ล็อกใน Express → ตั้งเป็น Closed (ไม่ downgrade งวดที่ปิด/ล็อกไว้แล้ว)
            if (p.Locked)
            {
                if (closingByKey.TryGetValue((year, periodNo), out var existing))
                {
                    if (existing.Status == PeriodStatus.Open)
                    {
                        existing.Status = PeriodStatus.Closed;
                        existing.ClosedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    db.ClosingPeriods.Add(new Domain.Entities.ClosingPeriod
                    {
                        ClientCompanyId = companyId,
                        Year = year,
                        Month = periodNo,
                        Status = PeriodStatus.Closed,
                        ClosedAt = DateTime.UtcNow,
                    });
                }
            }
        }
    }
}
