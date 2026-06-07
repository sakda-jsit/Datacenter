using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Datacenter.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Datacenter.Infrastructure.Services;

/// <summary>
/// เก็บ snapshot ไฟล์ DBF ต้นฉบับของ Express เป็น zip เดียวต่อการนำเข้า (หลักฐานเก็บถาวร docs/20).
/// อ่านไฟล์ด้วย FileShare.ReadWrite รองรับกรณี Express เปิดไฟล์ค้าง.
/// </summary>
public class ImportSnapshotService(IConfiguration configuration) : IImportSnapshotService
{
    private readonly string _basePath = configuration["Import:SnapshotBasePath"]
        ?? throw new InvalidOperationException("Import:SnapshotBasePath is not configured in appsettings.");

    /// <summary>
    /// ตาราง Express ที่ระบบใช้ (เก็บเป็นหลักฐาน). ไฟล์ที่ไม่พบจะถูกข้าม.
    /// แต่ละตารางเก็บไฟล์หลัก .DBF + ไฟล์ memo/index ที่มาคู่กัน (.FPT/.DBT/.CDX) เพื่อความครบถ้วน.
    /// </summary>
    private static readonly string[] ExpressTables =
    [
        "ISINFO", "ISPRD", "GLACC", "GLBAL", "GLJNLIT",
        "FAMAS", "ISVAT", "ISTAX",
        "ARMAS", "ARTRN", "APMAS", "APTRN",
        "STMAS", "BKMAS", "BKTRN",
    ];

    private static readonly string[] CompanionExtensions = [".DBF", ".FPT", ".DBT", ".CDX"];

    public async Task<ImportSnapshotCaptureResult> CaptureAsync(
        string companyFolderPath, string clientCode, int fiscalYear, CancellationToken ct = default)
    {
        // หาไฟล์ต้นฉบับทั้งหมด (case-insensitive) ของตารางที่ระบบใช้
        var sourceFiles = ResolveSourceFiles(companyFolderPath);

        var fileInfos = new List<ImportSnapshotFileInfo>();
        bool partial = false;
        var notes = new List<string>();
        long totalSourceBytes = 0;

        // เตรียมโฟลเดอร์ปลายทาง {base}\{clientCode}\{fiscalYear}\
        var relativeDir = Path.Combine(clientCode, fiscalYear.ToString());
        var targetDir = Path.Combine(_basePath, relativeDir);
        Directory.CreateDirectory(targetDir);

        var archiveFileName = $"snapshot_{fiscalYear}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}.zip";
        var archiveRelativePath = Path.Combine(relativeDir, archiveFileName);
        var archiveFullPath = Path.Combine(_basePath, archiveRelativePath);

        await using (var zipStream = new FileStream(archiveFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            foreach (var (table, fullPath, fileName) in sourceFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var bytes = ReadAllBytesShared(fullPath);
                    var sha = ToHex(SHA256.HashData(bytes));
                    int? rowCount = fileName.EndsWith(".DBF", StringComparison.OrdinalIgnoreCase)
                        ? TryReadDbfRecordCount(bytes)
                        : null;
                    var modified = SafeLastWriteTimeUtc(fullPath);

                    // เขียนไฟล์ลง zip โดยจัดกลุ่มในโฟลเดอร์ชื่อตาราง
                    var entry = zip.CreateEntry($"{table}/{fileName}", CompressionLevel.Optimal);
                    await using (var es = entry.Open())
                        await es.WriteAsync(bytes, ct);

                    fileInfos.Add(new ImportSnapshotFileInfo(table, fileName, bytes.LongLength, sha, rowCount, modified));
                    totalSourceBytes += bytes.LongLength;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    partial = true;
                    notes.Add($"{fileName}: {ex.Message}");
                }
            }

            // manifest อ่านง่ายสำหรับผู้สอบบัญชี (ฝังในไฟล์ zip)
            var manifestEntry = zip.CreateEntry("_manifest.txt", CompressionLevel.Optimal);
            await using (var ms = manifestEntry.Open())
            {
                var manifest = BuildManifest(companyFolderPath, clientCode, fiscalYear, fileInfos, notes);
                var manifestBytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(manifest);
                await ms.WriteAsync(manifestBytes, ct);
            }
        }

        // checksum + ขนาดของไฟล์ zip ทั้งก้อน
        var archiveBytes = await File.ReadAllBytesAsync(archiveFullPath, ct);
        var archiveSha = ToHex(SHA256.HashData(archiveBytes));

        return new ImportSnapshotCaptureResult(
            ArchiveRelativePath: archiveRelativePath,
            ArchiveFileName: archiveFileName,
            ArchiveByteSize: archiveBytes.LongLength,
            ArchiveSha256: archiveSha,
            FileCount: fileInfos.Count,
            TotalSourceBytes: totalSourceBytes,
            Partial: partial,
            Note: notes.Count > 0 ? string.Join("; ", notes) : null,
            Files: fileInfos);
    }

    public async Task<byte[]?> ReadArchiveAsync(string archiveRelativePath, CancellationToken ct = default)
    {
        var full = Path.Combine(_basePath, archiveRelativePath);
        if (!File.Exists(full)) return null;
        return await File.ReadAllBytesAsync(full, ct);
    }

    public bool DeleteArchive(string archiveRelativePath)
    {
        if (string.IsNullOrWhiteSpace(archiveRelativePath)) return false;
        var full = Path.Combine(_basePath, archiveRelativePath);
        if (!File.Exists(full)) return false;
        File.Delete(full);
        return true;
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static List<(string Table, string FullPath, string FileName)> ResolveSourceFiles(string folder)
    {
        var result = new List<(string, string, string)>();
        if (!Directory.Exists(folder)) return result;

        foreach (var table in ExpressTables)
        foreach (var ext in CompanionExtensions)
        {
            // จับคู่แบบ case-insensitive (Express ใช้ทั้งตัวพิมพ์ใหญ่/เล็ก)
            var matches = Directory.GetFiles(folder, $"{table}.*", SearchOption.TopDirectoryOnly)
                .Where(f => Path.GetExtension(f).Equals(ext, StringComparison.OrdinalIgnoreCase)
                         && Path.GetFileNameWithoutExtension(f).Equals(table, StringComparison.OrdinalIgnoreCase));
            foreach (var m in matches)
                result.Add((table, m, Path.GetFileName(m).ToUpperInvariant()));
        }

        return result;
    }

    private static byte[] ReadAllBytesShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[fs.Length];
        fs.ReadExactly(buffer);
        return buffer;
    }

    /// <summary>จำนวนระเบียนของ DBF จาก header (ไบต์ 4..8 little-endian). null ถ้า header สั้นเกินไป.</summary>
    private static int? TryReadDbfRecordCount(byte[] bytes)
        => bytes.Length >= 8 ? BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4)) : null;

    private static DateTime? SafeLastWriteTimeUtc(string path)
    {
        try { return File.GetLastWriteTimeUtc(path); }
        catch { return null; }
    }

    private static string ToHex(byte[] hash)
        => Convert.ToHexString(hash).ToLowerInvariant();

    private static string BuildManifest(
        string sourceFolder, string clientCode, int fiscalYear,
        IReadOnlyList<ImportSnapshotFileInfo> files, IReadOnlyList<string> notes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Express DBF Import Snapshot — หลักฐานการนำเข้า (เก็บถาวร 10 ปี)");
        sb.AppendLine(new string('=', 64));
        sb.AppendLine($"รหัสบริษัท (Express)  : {clientCode}");
        sb.AppendLine($"ปีบัญชี (AD)          : {fiscalYear}");
        sb.AppendLine($"โฟลเดอร์ต้นทาง        : {sourceFolder}");
        sb.AppendLine($"เก็บเมื่อ (UTC)        : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"จำนวนไฟล์             : {files.Count}");
        sb.AppendLine();
        sb.AppendLine($"{"ตาราง",-12}{"ไฟล์",-18}{"ระเบียน",10}{"ขนาด(ไบต์)",14}  SHA-256");
        sb.AppendLine(new string('-', 100));
        foreach (var f in files)
            sb.AppendLine($"{f.TableName,-12}{f.FileName,-18}{(f.RowCount?.ToString() ?? "-"),10}{f.ByteSize,14:N0}  {f.Sha256}");

        if (notes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("หมายเหตุ/ข้อผิดพลาด:");
            foreach (var n in notes) sb.AppendLine($"  - {n}");
        }

        return sb.ToString();
    }
}
