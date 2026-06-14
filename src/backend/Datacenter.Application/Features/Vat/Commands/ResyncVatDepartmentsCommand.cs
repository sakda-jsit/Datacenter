using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Vat.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Vat.Commands;

/// <summary>
/// ดึงรายการ VAT จาก Express ISVAT ใหม่ทุกบริษัท (เฉพาะ VAT — ไม่แตะ GL/posting/snapshot)
/// เพื่อเติม DEPCOD (สาขา) ให้ข้อมูลที่นำเข้าก่อนเพิ่มฟิลด์นี้. ใช้ครั้งเดียวหลังอัปเกรด.
/// แทนที่ VatEntries ทั้งชุดต่อบริษัทจากต้นทาง (idempotent).
/// </summary>
public record ResyncVatDepartmentsCommand : IRequest<ResyncVatDepartmentsResult>;

public record ResyncVatDepartmentsResult(
    int CompaniesProcessed, int CompaniesUpdated, int TotalEntries, int CompaniesWithBranches,
    IReadOnlyList<string> Skipped);

public class ResyncVatDepartmentsCommandHandler(
    IApplicationDbContext db, IExpressDbfAdapter dbfAdapter,
    IImportStorageService storage, ICurrentUserService currentUser, IAuditService audit)
    : IRequestHandler<ResyncVatDepartmentsCommand, ResyncVatDepartmentsResult>
{
    public async Task<ResyncVatDepartmentsResult> Handle(ResyncVatDepartmentsCommand req, CancellationToken ct)
    {
        // เฉพาะบริษัทที่มีข้อมูล VAT อยู่แล้ว (มี ISVAT)
        var companyIds = await db.VatEntries.AsNoTracking()
            .Select(v => v.ClientCompanyId).Distinct().ToListAsync(ct);

        int processed = 0, updated = 0, totalEntries = 0, withBranches = 0;
        var skipped = new List<string>();

        foreach (var cid in companyIds)
        {
            var company = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == cid, ct);
            if (company is null) continue;
            processed++;

            string folder;
            try { folder = storage.GetExpressFolderPath(company.Code); }
            catch { skipped.Add($"{company.Code}: ไม่พบโฟลเดอร์"); continue; }

            if (!await dbfAdapter.FolderIsValidAsync(folder, ct))
            {
                skipped.Add($"{company.Code}: โฟลเดอร์ไม่พร้อม");
                continue;
            }

            // batch ล่าสุดของบริษัท (ผูก ImportBatchId ของ VatEntry)
            var batchId = await db.ImportBatches.AsNoTracking()
                .Where(b => b.ClientCompanyId == cid)
                .OrderByDescending(b => b.Id).Select(b => (int?)b.Id).FirstOrDefaultAsync(ct);
            if (batchId is null) { skipped.Add($"{company.Code}: ไม่มี import batch"); continue; }

            try
            {
                var (read, _) = await VatEntryImporter.ImportAsync(
                    db, dbfAdapter, folder, cid, batchId.Value, currentUser.Username, ct);
                await db.SaveChangesAsync(ct);
                if (read > 0) updated++;
                totalEntries += read;

                var hasBranch = await db.VatEntries.AsNoTracking()
                    .Where(v => v.ClientCompanyId == cid && v.DepartmentCode != null && v.DepartmentCode != "")
                    .Select(v => v.DepartmentCode).Distinct().CountAsync(ct);
                if (hasBranch > 1) withBranches++;
            }
            catch (Exception ex)
            {
                skipped.Add($"{company.Code}: {ex.Message}");
            }
        }

        await audit.LogAsync("ResyncDepartments", "VatEntry",
            entityId: "all",
            afterValue: $"resync {updated}/{processed} บริษัท, {totalEntries} รายการ, มีหลายสาขา {withBranches}",
            cancellationToken: ct);
        await db.SaveChangesAsync(ct);

        return new ResyncVatDepartmentsResult(processed, updated, totalEntries, withBranches, skipped);
    }
}
