using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Import.DTOs;

public record ImportSnapshotDto(
    int Id,
    int ImportBatchId,
    int ClientCompanyId,
    int FiscalYear,
    DateTime CapturedAt,
    string SourceFolderPath,
    string ArchiveFileName,
    long ArchiveByteSize,
    string ArchiveSha256,
    int FileCount,
    long TotalSourceBytes,
    ImportSnapshotStatus Status,
    string? Note,
    DateTime RetainUntil,
    string CreatedBy,
    IReadOnlyList<ImportSnapshotFileDto> Files);

public record ImportSnapshotFileDto(
    string TableName,
    string FileName,
    long ByteSize,
    string Sha256,
    int? RowCount,
    DateTime? SourceModifiedAt);
