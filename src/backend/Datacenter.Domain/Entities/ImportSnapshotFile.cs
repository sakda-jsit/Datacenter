using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายไฟล์ DBF ต้นฉบับหนึ่งไฟล์ในชุด snapshot (Import Evidence Log รายตาราง)
/// — ชื่อตาราง/ไฟล์, ขนาด, checksum, จำนวนระเบียน ณ ตอนนำเข้า.
/// </summary>
public class ImportSnapshotFile : BaseEntity
{
    public int ImportSnapshotId { get; set; }

    /// <summary>ชื่อตาราง Express (เช่น GLBAL, ISVAT)</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>ชื่อไฟล์จริง (เช่น GLBAL.DBF, GLBAL.FPT)</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>ขนาดไฟล์ต้นฉบับ (ไบต์)</summary>
    public long ByteSize { get; set; }

    /// <summary>SHA-256 ของไฟล์ต้นฉบับ (hex ตัวพิมพ์เล็ก)</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>จำนวนระเบียนจาก header ของ DBF (null = ไม่ใช่ .DBF หรืออ่าน header ไม่ได้)</summary>
    public int? RowCount { get; set; }

    /// <summary>วันแก้ไขล่าสุดของไฟล์ต้นฉบับ (last write time)</summary>
    public DateTime? SourceModifiedAt { get; set; }

    public ImportSnapshot ImportSnapshot { get; set; } = null!;
}
