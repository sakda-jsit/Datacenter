using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// การยื่น สปส.1-10 รายเดือน (1:1 กับ PayrollRun) — ติดตามสถานะยื่น + ใบเสร็จ + กระทบยอด.
/// เก็บ snapshot ยอด ณ วันยื่น เพื่อ detect drift ถ้างวดถูกแก้ภายหลัง.
/// แบบที่ยื่น/ใบเสร็จ เก็บเป็น blob (nullable) บนเอนทิตีนี้.
/// </summary>
public class SsoMonthlyFiling : BaseEntity
{
    public int PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public int ClientCompanyId { get; set; }   // denormalize เพื่อ query/ตรวจสิทธิ์
    public int Year { get; set; }
    public int Month { get; set; }

    public SsoFilingStatus Status { get; set; } = SsoFilingStatus.NotFiled;

    // ── การยื่น ──
    public DateTime? SubmittedDate { get; set; }
    // snapshot ยอด ณ วันยื่น (เทียบกับยอดงวดปัจจุบันเพื่อ flag drift)
    public int SnapshotEmployeeCount { get; set; }
    public decimal SnapshotTotalWage { get; set; }
    public decimal SnapshotEmployeeContribution { get; set; }
    public decimal SnapshotEmployerContribution { get; set; }
    public decimal SnapshotGrandTotal { get; set; }

    // ── ใบเสร็จ ──
    public DateTime? ReceiptDate { get; set; }
    public decimal? ReceiptAmount { get; set; }
    public string? ReceiptNo { get; set; }

    public string? Note { get; set; }

    // ── เอกสารแนบ (blob) ──
    public string? FormFileName { get; set; }
    public string? FormContentType { get; set; }
    public byte[]? FormContent { get; set; }

    public string? ReceiptFileName { get; set; }
    public string? ReceiptContentType { get; set; }
    public byte[]? ReceiptContent { get; set; }
}
