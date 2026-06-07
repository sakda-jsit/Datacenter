namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// เก็บ snapshot ไฟล์ DBF ต้นฉบับของ Express เป็นหลักฐานเก็บถาวร (docs/20).
/// IO (อ่านโฟลเดอร์/hash/zip) อยู่ใน Infrastructure; การบันทึก entity อยู่ใน Application.
/// พาธฐานมาจาก config "Import:SnapshotBasePath".
/// </summary>
public interface IImportSnapshotService
{
    /// <summary>
    /// บีบไฟล์ DBF ต้นฉบับที่ระบบใช้ (ที่พบในโฟลเดอร์) เป็น zip เดียว พร้อม manifest
    /// แล้วคืน metadata + รายไฟล์ + checksum. ไม่ throw ถ้าไม่มีไฟล์ — คืน FileCount=0.
    /// </summary>
    Task<ImportSnapshotCaptureResult> CaptureAsync(
        string companyFolderPath, string clientCode, int fiscalYear, CancellationToken ct = default);

    /// <summary>อ่านไฟล์ zip กลับมาเป็น bytes (null ถ้าไม่พบไฟล์).</summary>
    Task<byte[]?> ReadArchiveAsync(string archiveRelativePath, CancellationToken ct = default);

    /// <summary>ลบไฟล์ zip ที่เก็บไว้ (เงียบถ้าไม่มี). คืน true เมื่อมีไฟล์และลบสำเร็จ.</summary>
    bool DeleteArchive(string archiveRelativePath);
}

/// <summary>ผลการเก็บ snapshot — header + รายไฟล์ (ยังไม่ผูก entity).</summary>
public record ImportSnapshotCaptureResult(
    string ArchiveRelativePath,
    string ArchiveFileName,
    long ArchiveByteSize,
    string ArchiveSha256,
    int FileCount,
    long TotalSourceBytes,
    bool Partial,
    string? Note,
    IReadOnlyList<ImportSnapshotFileInfo> Files);

/// <summary>metadata ไฟล์ต้นฉบับหนึ่งไฟล์ในชุด snapshot.</summary>
public record ImportSnapshotFileInfo(
    string TableName,
    string FileName,
    long ByteSize,
    string Sha256,
    int? RowCount,
    DateTime? SourceModifiedAt);
