using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// บรรทัด schedule ในแบบ ภ.ง.ด.50 (รายการที่ 4-11) ที่รับการ map จากบัญชี — master taxonomy (ใช้ทุกบริษัท).
/// เก็บพิกัด field บน PDF ไว้ในตัว (PdfPage 0-based + X/Y/W) เพื่อให้ Pnd50PdfService วาดได้แบบ generic.
/// IsTotal = บรรทัดผลรวม (คำนวณ ไม่รับ map). IsCatchAll = บรรทัด "อื่นๆ" (บัญชีที่ไม่ถูก map มาลงที่นี่).
/// </summary>
public class Cit50ScheduleLine : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // เช่น R8_RENT
    public int ScheduleNo { get; set; }                    // 8 = รายการที่ 8
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public int PdfPage { get; set; }                       // 0-based (รายการ 8 = หน้า 5 → 4)
    public double PdfX { get; set; }
    public double PdfY { get; set; }
    public double PdfW { get; set; }

    public bool IsTotal { get; set; }
    public bool IsCatchAll { get; set; }
}
