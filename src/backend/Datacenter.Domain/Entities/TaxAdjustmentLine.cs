using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายการปรับปรุงทางภาษีหนึ่งบรรทัด — บวกกลับ (ค่าใช้จ่ายต้องห้าม) หรือ หักออก (รายได้ยกเว้น/หักได้เพิ่ม).
/// </summary>
public class TaxAdjustmentLine : BaseEntity
{
    public int TaxComputationId { get; set; }

    public TaxAdjustmentKind Kind { get; set; }

    /// <summary>คำอธิบายรายการ (เช่น "ค่ารับรองส่วนเกิน", "เบี้ยปรับ/เงินเพิ่ม", "รายได้ที่ได้รับยกเว้น")</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>จำนวนเงิน (เป็นบวกเสมอ; ทิศทางกำหนดด้วย Kind)</summary>
    public decimal Amount { get; set; }

    public int SortOrder { get; set; }

    public TaxComputation TaxComputation { get; set; } = null!;
}
