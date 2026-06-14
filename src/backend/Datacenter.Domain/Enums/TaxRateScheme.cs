namespace Datacenter.Domain.Enums;

/// <summary>
/// อัตราภาษีเงินได้นิติบุคคลที่ใช้คำนวณ ภ.ง.ด.50.
/// เก็บเป็น int (เลี่ยง LINQ-cast gotcha ของ enum-as-string).
/// </summary>
public enum TaxRateScheme
{
    /// <summary>
    /// นิติบุคคล SME (ทุนชำระแล้ว ≤ 5 ล้าน และรายได้ ≤ 30 ล้าน) — อัตราขั้นบันได:
    /// 0–300,000 ยกเว้น (0%), 300,001–3,000,000 = 15%, ส่วนเกิน 3,000,000 = 20%.
    /// </summary>
    SmeTiered = 1,

    /// <summary>นิติบุคคลทั่วไป — 20% ของกำไรสุทธิทางภาษีทั้งจำนวน</summary>
    Flat20 = 2,

    /// <summary>กำหนดอัตราเดียวเอง (เช่น สิทธิ BOI / กิจการพิเศษ) — ใช้ CustomRatePct</summary>
    Custom = 3,
}
