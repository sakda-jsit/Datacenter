using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Import.Queries;

/// <summary>ดาวน์โหลดไฟล์ zip หลักฐานการนำเข้า (ไฟล์ DBF ต้นฉบับ) ของ batch หนึ่ง</summary>
public record DownloadImportSnapshotQuery(int ImportBatchId) : IRequest<ImportSnapshotDownloadDto>;

public record ImportSnapshotDownloadDto(byte[] Content, string FileName);

public class DownloadImportSnapshotQueryHandler(
    IApplicationDbContext db,
    ICompanyAccessGuard accessGuard,
    IImportSnapshotService snapshotService)
    : IRequestHandler<DownloadImportSnapshotQuery, ImportSnapshotDownloadDto>
{
    public async Task<ImportSnapshotDownloadDto> Handle(DownloadImportSnapshotQuery request, CancellationToken ct)
    {
        var batch = await db.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.ImportBatchId, ct)
            ?? throw new NotFoundException("ImportBatch", request.ImportBatchId);

        await accessGuard.EnsureAccessAsync(batch.ClientCompanyId, ct);

        var snapshot = await db.ImportSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ImportBatchId == request.ImportBatchId, ct)
            ?? throw new NotFoundException("ImportSnapshot", request.ImportBatchId);

        var bytes = await snapshotService.ReadArchiveAsync(snapshot.ArchiveRelativePath, ct)
            ?? throw new NotFoundException("ImportSnapshot archive", snapshot.ArchiveFileName);

        return new ImportSnapshotDownloadDto(bytes, snapshot.ArchiveFileName);
    }
}
