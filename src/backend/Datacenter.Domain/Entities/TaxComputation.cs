using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// กระดาษทำการคำนวณภาษีเงินได้นิติบุคคล (ภ.ง.ด.50) ต่อ (บริษัท, ปีงบ).
/// ต่อยอดจากกำไรสุทธิทางบัญชีก่อนภาษี (จากงบกำไรขาดทุน) → +บวกกลับ −หักออก
/// → หักผลขาดทุนสะสมยกมา → คำนวณภาษีตามอัตรา → หักภาษีจ่ายล่วงหน้า (WHT).
/// ภาษีที่คำนวณได้จะถูก mirror ไปยัง FsExternalInput (X4) เพื่อให้งบดุลลง counterpart (TXP/TXR) เดิม.
/// ป้อนมือ (Express ไม่มีทะเบียนการคำนวณภาษี) — แก้ไข/ลบได้ทุกคนที่มีสิทธิ์ + audit.
/// </summary>
public class TaxComputation : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ปีบัญชี (ค.ศ.)</summary>
    public int FiscalYear { get; set; }

    /// <summary>อัตราภาษีที่ใช้ (SME ขั้นบันได / 20% / กำหนดเอง)</summary>
    public TaxRateScheme RateScheme { get; set; } = TaxRateScheme.SmeTiered;

    /// <summary>อัตราภาษี % (ใช้เมื่อ RateScheme = Custom)</summary>
    public decimal? CustomRatePct { get; set; }

    /// <summary>ผลขาดทุนสะสมยกมา (ที่ยังใช้สิทธิได้ ≤ 5 ปี) — หักก่อนคำนวณภาษี</summary>
    public decimal LossBroughtForward { get; set; }

    /// <summary>ภาษีจ่ายล่วงหน้า/ถูกหัก ณ ที่จ่าย (WHT) ที่นำมาหักจากภาษีที่ต้องชำระ</summary>
    public decimal WhtCredit { get; set; }

    public string? Note { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
    public List<TaxAdjustmentLine> Lines { get; set; } = new();
}
