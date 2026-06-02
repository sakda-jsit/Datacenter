using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

public class ImportBatch : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public ImportSourceType SourceType { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public int FiscalYear { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public string? Message { get; set; }
    public DateTime? FinishedAt { get; set; }

    // Posting: ยกข้อมูลจาก staging ไปยังตารางจริง (Account + JournalEntry) แล้วหรือยัง
    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
