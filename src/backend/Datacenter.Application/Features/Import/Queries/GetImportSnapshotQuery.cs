using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using Datacenter.Application.Features.Import.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Queries;

/// <summary>ดึง metadata + รายไฟล์ของ snapshot หลักฐานการนำเข้าของ batch หนึ่ง (null ถ้ายังไม่มี snapshot)</summary>
public record GetImportSnapshotQuery(int ImportBatchId) : IRequest<ImportSnapshotDto?>;

public class GetImportSnapshotQueryHandler(IApplicationDbContext db, ICompanyAccessGuard accessGuard)
    : IRequestHandler<GetImportSnapshotQuery, ImportSnapshotDto?>
{
    public async Task<ImportSnapshotDto?> Handle(GetImportSnapshotQuery request, CancellationToken ct)
    {
        var batch = await db.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, ct)
            ?? throw new NotFoundException("ImportBatch", request.ImportBatchId);

        await accessGuard.EnsureAccessAsync(batch.ClientCompanyId, ct);

        var snapshot = await db.ImportSnapshots
            .AsNoTracking()
            .Include(s => s.Files)
            .FirstOrDefaultAsync(s => s.ImportBatchId == request.ImportBatchId, ct);

        if (snapshot is null) return null;

        return new ImportSnapshotDto(
            snapshot.Id,
            snapshot.ImportBatchId,
            snapshot.ClientCompanyId,
            snapshot.FiscalYear,
            snapshot.CapturedAt,
            snapshot.SourceFolderPath,
            snapshot.ArchiveFileName,
            snapshot.ArchiveByteSize,
            snapshot.ArchiveSha256,
            snapshot.FileCount,
            snapshot.TotalSourceBytes,
            snapshot.Status,
            snapshot.Note,
            snapshot.RetainUntil,
            snapshot.CreatedBy,
            snapshot.Files
                .OrderBy(f => f.TableName).ThenBy(f => f.FileName)
                .Select(f => new ImportSnapshotFileDto(
                    f.TableName, f.FileName, f.ByteSize, f.Sha256, f.RowCount, f.SourceModifiedAt))
                .ToList());
    }
}
