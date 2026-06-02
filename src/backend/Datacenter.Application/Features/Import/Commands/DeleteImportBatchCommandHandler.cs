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

        // ลบได้เฉพาะปีที่อยู่ในนิยามรอบบัญชีปัจจุบัน (ISPRD) — ปีที่หลุดออกจากรอบบัญชีเป็นข้อมูลประวัติ ห้ามลบ
        // ถ้ายังไม่เคยนำเข้านิยามรอบบัญชี (ไม่มี AccountingPeriod เลย) จะไม่บังคับ เพื่อไม่ให้ข้อมูลเดิมลบไม่ได้
        var definedYears = await db.AccountingPeriods
            .Where(p => p.ClientCompanyId == batch.ClientCompanyId)
            .Select(p => p.Year)
            .Distinct()
            .ToListAsync(ct);
        if (definedYears.Count > 0 && !definedYears.Contains(batch.FiscalYear))
            throw new DomainException(
                $"ปี {batch.FiscalYear} ไม่อยู่ในรอบบัญชีปัจจุบัน จึงไม่สามารถลบข้อมูลได้ " +
                $"(ปีที่ลบได้: {string.Join(", ", definedYears.OrderBy(y => y))})");

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
}
