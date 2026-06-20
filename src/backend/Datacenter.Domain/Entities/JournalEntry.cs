using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime JournalDate { get; set; }
    /// <summary>
    /// ปีงบที่รายการนี้สังกัด (explicit) — ใช้กรองยอดต่อปีแทนการเดาจาก JournalDate+SourceModule.
    /// OPEN-Y และ MOVE-Y มี FiscalYear = Y เท่ากัน (แม้ OPEN-Y ลงวันที่ 31/12/(Y-1)) จึงไม่ชนกับ
    /// OPEN-(Y+1) ที่ลงวันที่เดียวกับ MOVE-Y อีก (ดู FsJournalNets / fs-cumulative-double-count).
    /// </summary>
    public int FiscalYear { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
    public ICollection<JournalEntryLine> Lines { get; set; } = [];
}
