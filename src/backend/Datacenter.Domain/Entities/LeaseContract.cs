using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// สัญญาเช่าซื้อ/เงินกู้ (กระดาษทำการปิดงบ — req v11 docs/13 ข้อ 2).
/// ระบบคำนวณตารางตัดบัญชีแบบ effective-interest จากพารามิเตอร์สัญญา (ไม่เก็บ schedule —
/// คำนวณสดเหมือน Adjusted TB) แล้ว generate รายการปรับปรุง (AdjustmentEntry) เข้า TB ปีปัจจุบัน.
/// </summary>
public class LeaseContract : BaseEntity
{
    public int ClientCompanyId { get; set; }

    public LeaseContractType ContractType { get; set; } = LeaseContractType.HirePurchase;

    /// <summary>เลขที่สัญญา (เช่น E057-2025)</summary>
    public string ContractNo { get; set; } = string.Empty;

    /// <summary>ชื่อทรัพย์สิน/รายการ (เช่น SOLAR ROOFTOP)</summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>รหัสทรัพย์สิน (ถ้ามี)</summary>
    public string? AssetCode { get; set; }

    /// <summary>ผู้ให้เช่า/เจ้าหนี้ (ชื่อบริษัทไฟแนนซ์/ผู้ให้กู้)</summary>
    public string? Lessor { get; set; }

    public DateTime ContractDate { get; set; }

    /// <summary>วันครบกำหนดงวดแรก</summary>
    public DateTime FirstInstallmentDate { get; set; }

    /// <summary>จำนวนงวดทั้งหมด</summary>
    public int NumberOfPeriods { get; set; }

    /// <summary>จำนวนงวดต่อปี (รายเดือน = 12)</summary>
    public int PaymentsPerYear { get; set; } = 12;

    /// <summary>ราคาเงินสด (ไม่รวม VAT) — เชิงอ้างอิง</summary>
    public decimal CashPrice { get; set; }

    /// <summary>เงินดาวน์ (ไม่รวม VAT) — เชิงอ้างอิง</summary>
    public decimal DownPayment { get; set; }

    /// <summary>เงินต้นที่จัดไฟแนนซ์ (ไม่รวม VAT) = ราคาเงินสด − เงินดาวน์</summary>
    public decimal FinancedPrincipal { get; set; }

    /// <summary>ค่างวดต่อเดือน ไม่รวม VAT (เงินต้น + ดอกเบี้ย)</summary>
    public decimal InstallmentAmount { get; set; }

    /// <summary>ภาษีซื้อต่องวด (เช่าซื้อ); เงินกู้ = 0</summary>
    public decimal VatPerPeriod { get; set; }

    // ── ผูกบัญชี GL สำหรับ generate adjustment ────────────────────────────────
    /// <summary>บัญชีหนี้สินตามสัญญาเช่าซื้อ/เงินกู้ (gross) — เช่น 2120-05</summary>
    public int LiabilityAccountId { get; set; }

    /// <summary>บัญชีดอกเบี้ยเช่าซื้อรอตัดบัญชี (contra) — เช่น 1157-00; เงินกู้ = null</summary>
    public int? DeferredInterestAccountId { get; set; }

    /// <summary>บัญชีภาษีซื้อยังไม่ถึงกำหนด — เช่น 1155-00; เงินกู้ = null</summary>
    public int? InputVatUndueAccountId { get; set; }

    /// <summary>บัญชีดอกเบี้ยจ่าย (P&amp;L) — ปลายทางการรับรู้ดอกเบี้ยในปี</summary>
    public int InterestExpenseAccountId { get; set; }

    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsActive { get; set; } = true;

    public ClientCompany ClientCompany { get; set; } = null!;
}
