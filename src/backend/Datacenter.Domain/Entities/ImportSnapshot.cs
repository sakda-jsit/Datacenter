using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// หลักฐานการนำเข้า (Import Evidence) — snapshot ไฟล์ DBF ต้นฉบับของ Express ณ รอบปิดงบ
/// เก็บถาวรอย่างน้อย 10 ปี (req v11 #10/#13, docs/20) เพื่อให้ยอดที่ปิดงบแล้ว
/// ตรวจสอบย้อนกลับได้แม้ Express จะถูกแก้ภายหลัง. 1:1 กับ ImportBatch.
///
/// ไฟล์ต้นฉบับถูกบีบเป็น zip เดียวเก็บบน filesystem (Import:SnapshotBasePath);
/// metadata + checksum + รายไฟล์เก็บในฐานข้อมูลเป็น audit/evidence log.
/// </summary>
public class ImportSnapshot : BaseEntity
{
    public int ImportBatchId { get; set; }
    public int ClientCompanyId { get; set; }
    public int FiscalYear { get; set; }

    /// <summary>วันเวลาที่ดึง snapshot (= เวลานำเข้า)</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>โฟลเดอร์ Express ต้นทางที่อ่าน (เช่น J:\JSIT2016)</summary>
    public string SourceFolderPath { get; set; } = string.Empty;

    /// <summary>พาธไฟล์ zip (สัมพัทธ์กับ SnapshotBasePath) เพื่อให้ย้าย base ได้</summary>
    public string ArchiveRelativePath { get; set; } = string.Empty;

    /// <summary>ชื่อไฟล์ zip (สำหรับดาวน์โหลด)</summary>
    public string ArchiveFileName { get; set; } = string.Empty;

    /// <summary>ขนาดไฟล์ zip (ไบต์)</summary>
    public long ArchiveByteSize { get; set; }

    /// <summary>SHA-256 ของไฟล์ zip ทั้งก้อน (hex ตัวพิมพ์เล็ก)</summary>
    public string ArchiveSha256 { get; set; } = string.Empty;

    /// <summary>จำนวนไฟล์ต้นฉบับที่เก็บได้</summary>
    public int FileCount { get; set; }

    /// <summary>ขนาดรวมของไฟล์ต้นฉบับก่อนบีบ (ไบต์)</summary>
    public long TotalSourceBytes { get; set; }

    public ImportSnapshotStatus Status { get; set; } = ImportSnapshotStatus.Captured;
    public string? Note { get; set; }

    /// <summary>เก็บอย่างน้อยถึงวันนี้ (= CapturedAt + 10 ปี) — ห้ามลบก่อนกำหนดตามกฎหมายบัญชี/สรรพากร</summary>
    public DateTime RetainUntil { get; set; }

    public ImportBatch ImportBatch { get; set; } = null!;
    public ClientCompany ClientCompany { get; set; } = null!;
    public List<ImportSnapshotFile> Files { get; set; } = new();
}
