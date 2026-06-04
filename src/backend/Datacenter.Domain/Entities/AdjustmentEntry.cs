using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายการปรับปรุงปิดงบ (closing adjustment) — แยกชั้นจากยอด Express ที่นำเข้า (JournalEntry).
/// กระดาษทำการปิดงบ (TB25/TB24) แสดงคอลัมน์ "Adj" จากรายการเหล่านี้ และยอดหลังปรับปรุง = ยอดก่อนปรับ + Adj.
/// ต้องสมดุล (รวม Debit = รวม Credit). อ้างอิง docs/13 ข้อ 2.
/// </summary>
public class AdjustmentEntry : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ปีงบที่รายการนี้ปรับปรุง (adjustment เข้า TB ปีปัจจุบัน)</summary>
    public int FiscalYear { get; set; }

    /// <summary>เลขที่เอกสารปรับปรุง เช่น ADJ-2025-0001</summary>
    public string DocumentNo { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; }

    public AdjustmentSourceType SourceType { get; set; } = AdjustmentSourceType.Manual;

    /// <summary>อ้างอิงเอกสาร/กระดาษทำการต้นทาง (เลขสัญญา, เลขที่ schedule ฯลฯ)</summary>
    public string? Reference { get; set; }

    /// <summary>เหตุผลการปรับปรุง</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>พาธไฟล์แนบหลักฐาน (โมดูล evidence เต็มรูปอยู่ใน docs/18 — ตอนนี้เก็บ path/หมายเหตุ)</summary>
    public string? AttachmentPath { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
    public ICollection<AdjustmentEntryLine> Lines { get; set; } = [];
}
