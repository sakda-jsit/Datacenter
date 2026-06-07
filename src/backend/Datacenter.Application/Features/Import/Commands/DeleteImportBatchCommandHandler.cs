using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Commands;

public class DeleteImportBatchCommandHandler(
    IApplicationDbContext db,
    ICompanyAccessGuard accessGuard,
    IImportStorageService storage,
    IExpressDbfAdapter dbfAdapter,
    IImportSnapshotService snapshotService,
    IAuditService audit)
    : IRequestHandler<DeleteImportBatchCommand>
{
    public async Task Handle(DeleteImportBatchCommand request, CancellationToken ct)
    {
        var batch = await db.ImportBatches
            .FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, ct)
            ?? throw new NotFoundException("ImportBatch", request.ImportBatchId);

        // batch อ้างบริษัทผ่าน id จึงตรวจสิทธิ์หลังโหลด entity
        await accessGuard.EnsureAccessAsync(batch.ClientCompanyId, ct);

        // ลบได้เฉพาะปีที่อยู่ใน "หน้ากำหนดรอบบัญชี" (ISPRD) ปัจจุบันเท่านั้น — ปีที่หลุดออกจากหน้านี้แล้ว
        // = ข้อมูลประวัติ ห้ามลบ (Express โพสต์รอบปีแล้ว detail ถูกลบ → สำเนาที่นำเข้านี้เป็นชุดเดียวที่เหลือ)
        // อิง ISPRD สดเหมือนตอน import เพื่อให้ทั้งสองฝั่งผูกกับ "หน้าจอเดียวกัน"
        var allowedYears = await ResolveCurrentCycleYearsAsync(batch.ClientCompanyId, ct);
        if (allowedYears is not null && !allowedYears.Contains(batch.FiscalYear))
            throw new DomainException(
                $"ปี {batch.FiscalYear} ไม่อยู่ในรอบบัญชีปัจจุบัน (หน้ากำหนดรอบบัญชีของ Express) " +
                $"จึงเป็นข้อมูลประวัติที่ลบไม่ได้ (รอบที่ลบได้: {string.Join(", ", allowedYears.OrderBy(y => y))})");

        var batchId = batch.Id;
        var companyId = batch.ClientCompanyId;
        bool wasPosted = batch.IsPosted;

        // ถ้า post แล้ว ลบ JournalEntry ที่สร้างจาก batch นี้ (JournalEntryLine ลบตาม cascade)
        if (wasPosted)
        {
            var entries = await db.JournalEntries
                .Include(j => j.Lines)
                .Where(j => j.ImportBatchId == batchId)
                .ToListAsync(ct);
            if (entries.Count > 0)
                db.JournalEntries.RemoveRange(entries);
        }

        // ลบข้อมูล staging + validation details ของ batch
        var details = await db.ImportBatchDetails.Where(d => d.ImportBatchId == batchId).ToListAsync(ct);
        if (details.Count > 0) db.ImportBatchDetails.RemoveRange(details);

        var stagingAccounts = await db.StagingAccounts.Where(s => s.ImportBatchId == batchId).ToListAsync(ct);
        if (stagingAccounts.Count > 0) db.StagingAccounts.RemoveRange(stagingAccounts);

        var stagingTb = await db.StagingTrialBalances.Where(s => s.ImportBatchId == batchId).ToListAsync(ct);
        if (stagingTb.Count > 0) db.StagingTrialBalances.RemoveRange(stagingTb);

        // ลบไฟล์ zip หลักฐาน + row snapshot ของ batch นี้ (snapshot row/ไฟล์ลูกลบตาม cascade)
        var snapshots = await db.ImportSnapshots.Where(s => s.ImportBatchId == batchId).ToListAsync(ct);
        foreach (var s in snapshots)
            snapshotService.DeleteArchive(s.ArchiveRelativePath);
        if (snapshots.Count > 0) db.ImportSnapshots.RemoveRange(snapshots);

        await audit.LogAsync(
            action: "DeleteImport",
            entityName: "ImportBatch",
            entityId: batchId.ToString(),
            beforeValue: $"FY{batch.FiscalYear}, posted={wasPosted}",
            companyId: companyId,
            cancellationToken: ct);

        db.ImportBatches.Remove(batch);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// คืนปีที่อยู่ในรอบบัญชีปัจจุบัน (= ปีบน "หน้ากำหนดรอบบัญชี" ISPRD) สำหรับใช้เป็นด่านลบ:
    /// อ่าน ISPRD สดจาก Express ก่อน (แหล่งจริงของหน้าจอ); ถ้าโฟลเดอร์/ไฟล์อ่านไม่ได้
    /// จึง fallback เป็นนิยามรอบบัญชีที่เก็บไว้ล่าสุดใน DB (กันลบไม่ได้เมื่อ Express ออฟไลน์).
    /// คืน null = ยังไม่มีนิยามรอบบัญชีเลย → ไม่บังคับด่าน (ไม่ให้ข้อมูลเดิมลบไม่ได้).
    /// </summary>
    private async Task<IReadOnlyList<int>?> ResolveCurrentCycleYearsAsync(int companyId, CancellationToken ct)
    {
        var client = await db.ClientCompanies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, ct);

        if (client is not null && storage.ExpressFolderExists(client.Code))
        {
            try
            {
                var folder = storage.GetExpressFolderPath(client.Code);
                var periods = await dbfAdapter.ReadAccountingPeriodsAsync(folder, ct);
                var liveYears = periods.Select(p => p.EndDate.Year).Distinct().ToList();
                if (liveYears.Count > 0) return liveYears;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // อ่าน ISPRD ไม่ได้ → ใช้ค่าที่เก็บไว้แทน
            }
        }

        var storedYears = await db.AccountingPeriods
            .Where(p => p.ClientCompanyId == companyId)
            .Select(p => p.Year)
            .Distinct()
            .ToListAsync(ct);
        return storedYears.Count > 0 ? storedYears : null;
    }
}
