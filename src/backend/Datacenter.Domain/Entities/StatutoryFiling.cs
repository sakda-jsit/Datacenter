using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ติดตามสถานะการยื่นแบบภาษี/เงินสมทบทั่วไป (ภ.ง.ด.1 รายเดือน, ภ.ง.ด.1ก รายปี, กท.20ก รายปี).
/// keyed by (ClientCompanyId, FilingType, Year, Month) — Month=0 = แบบรายปี.
/// เก็บ snapshot ยอด ณ วันยื่น + ใบเสร็จ + เอกสารแนบ (blob). โครงเดียวกับ SsoMonthlyFiling
/// แต่ generic ไม่ผูก PayrollRun (รองรับแบบรายปี).
/// </summary>
public class StatutoryFiling : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public StatutoryFilingType FilingType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }   // 0 = รายปี

    public FilingStatus Status { get; set; } = FilingStatus.NotFiled;

    public DateTime? SubmittedDate { get; set; }
    // snapshot ยอด ณ วันยื่น (เทียบยอดปัจจุบันเพื่อ flag drift)
    public decimal SnapshotBase { get; set; }     // เงินได้ (PND) / ค่าจ้าง (กท.20)
    public decimal SnapshotAmount { get; set; }   // ภาษี (PND) / เงินสมทบ (กท.20)
    public int SnapshotCount { get; set; }        // จำนวนคน

    public DateTime? ReceiptDate { get; set; }
    public decimal? ReceiptAmount { get; set; }
    public string? ReceiptNo { get; set; }

    public string? Note { get; set; }

    public string? FormFileName { get; set; }
    public string? FormContentType { get; set; }
    public byte[]? FormContent { get; set; }

    public string? ReceiptFileName { get; set; }
    public string? ReceiptContentType { get; set; }
    public byte[]? ReceiptContent { get; set; }
}
