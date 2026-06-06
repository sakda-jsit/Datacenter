using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>งวดเงินเดือนรายเดือนของบริษัทลูกค้า (1 บริษัท + ปี + เดือน)</summary>
public class PayrollRun : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }   // 1-12 (ปฏิทิน)
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public string? Note { get; set; }

    public ICollection<PayrollItem> Items { get; set; } = [];
}
