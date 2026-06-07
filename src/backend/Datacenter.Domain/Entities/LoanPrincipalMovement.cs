using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายการเคลื่อนไหวเงินต้นของเงินให้กู้: + = ให้กู้เพิ่ม, − = รับชำระคืน.
/// ยอดเงินต้นคงเหลือ ณ เวลาใด = ผลรวม Amount ของ movement ที่วันที่ ≤ เวลานั้น.
/// </summary>
public class LoanPrincipalMovement : BaseEntity
{
    public int InterestBearingLoanId { get; set; }

    public DateTime Date { get; set; }

    /// <summary>จำนวน (บวก = ให้กู้เพิ่ม, ลบ = รับคืน)</summary>
    public decimal Amount { get; set; }

    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public InterestBearingLoan Loan { get; set; } = null!;
}
