using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายการตรวจนับเงินสดหนึ่งชนิด: มูลค่าหน้าตั๋ว (ธนบัตร/เหรียญ) × จำนวน = มูลค่ารวม.
/// </summary>
public class CashCountLine : BaseEntity
{
    public int CashCountId { get; set; }

    /// <summary>มูลค่าหน้าตั๋ว เช่น 1000, 500, 100, 1, 0.25</summary>
    public decimal Denomination { get; set; }

    /// <summary>จำนวนฉบับ/เหรียญ</summary>
    public int Quantity { get; set; }

    public int SortOrder { get; set; }

    /// <summary>มูลค่ารวมของชนิดนี้ (= Denomination × Quantity)</summary>
    public decimal Amount => Math.Round(Denomination * Quantity, 2);

    public CashCount CashCount { get; set; } = null!;
}
