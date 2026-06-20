using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>รายการย่อย (checklist) ของงาน — แตกงานใหญ่เป็นขั้นตอนย่อย + ติ๊กเสร็จทีละข้อ</summary>
public class WorkTaskItem : BaseEntity
{
    public int WorkTaskId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public int SortOrder { get; set; }

    public WorkTask WorkTask { get; set; } = null!;
}
