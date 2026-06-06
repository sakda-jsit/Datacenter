using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// เชื่อมรายการที่ระบบเงินเดือนคำนวณได้ ↔ การคีย์ลง Express (ปลายทางบัญชี).
/// ติดตามว่า "คีย์ลง Express แล้ว/ยัง" + เลขที่เอกสาร + ยอดที่คีย์ → กระทบยอดกับยอดที่ควรลง.
/// keyed (ClientCompanyId, SourceType, Year, Month) — Month=0 = รายปี.
/// หมายเหตุ: ระบบไม่เขียนกลับ DBF Express (อ่านอย่างเดียว) — นี่คือการติดตามสถานะ manual.
/// </summary>
public class ExpressPostingLink : BaseEntity
{
    public int ClientCompanyId { get; set; }
    public ExpressPostingSourceType SourceType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }   // 0 = รายปี

    public DateTime? PostedDate { get; set; }      // null = ยังไม่คีย์ลง Express
    public string? ExpressDocNo { get; set; }      // เลขที่เอกสาร/ใบสำคัญใน Express
    public decimal? PostedAmount { get; set; }     // ยอดที่คีย์จริง (เทียบยอดที่ควรลง)
    public string? Note { get; set; }
}
