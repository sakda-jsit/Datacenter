using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ชุดรายงานงบการเงินต่อ (บริษัท, ปีงบ, version) — req v11 #9.
/// เก็บ snapshot ชื่อ/เลขผู้เสียภาษี/ที่อยู่บริษัท + ยอดสรุปงบ ณ ตอน finalize
/// เพื่อให้งบที่ยื่นไปแล้วไม่เปลี่ยนแม้ข้อมูลบริษัท/บัญชีจะแก้ภายหลัง.
/// </summary>
public class ReportPackage : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public int FiscalYear { get; set; }

    /// <summary>เวอร์ชัน (เริ่ม 1; ยื่นเพิ่มเติม = version ใหม่, version เดิม freeze)</summary>
    public int Version { get; set; }

    public ReportPackageStatus Status { get; set; } = ReportPackageStatus.Draft;
    public string? Title { get; set; }
    public string? Note { get; set; }

    // ── Snapshot อัตลักษณ์บริษัท (เก็บตอน finalize) ──────────────────────────────
    public string? SnapshotCompanyName { get; set; }
    public string? SnapshotTaxId { get; set; }
    public string? SnapshotBranchCode { get; set; }
    public string? SnapshotAddress { get; set; }

    // ── Snapshot ยอดสรุปงบ (เก็บตอน finalize) ────────────────────────────────────
    public decimal? TotalAssets { get; set; }
    public decimal? TotalLiabilities { get; set; }
    public decimal? TotalEquity { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? NetProfit { get; set; }

    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
