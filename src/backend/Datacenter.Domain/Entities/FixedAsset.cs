using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ทะเบียนสินทรัพย์ถาวร (FA) — source of truth (req v11 docs/14).
/// คำนวณค่าเสื่อม "สด" แบบเส้นตรงทั้งชุดบัญชีและชุดภาษี (ไม่เก็บ schedule เหมือน LeaseContract/Adjusted TB)
/// แล้ว generate รายการปรับปรุงค่าเสื่อม (AdjustmentEntry) เข้า TB ปีปัจจุบัน.
/// </summary>
public class FixedAsset : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสสินทรัพย์ (เช่น FA-001)</summary>
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>ชื่อ/รายการสินทรัพย์</summary>
    public string AssetName { get; set; } = string.Empty;

    /// <summary>ประเภทสินทรัพย์ (มาสเตอร์) — ใช้ default อัตราค่าเสื่อม</summary>
    public int? AssetTypeId { get; set; }

    public DateTime AcquireDate { get; set; }

    /// <summary>ราคาทุน</summary>
    public decimal Cost { get; set; }

    /// <summary>มูลค่าซาก (depreciable base = Cost − SalvageValue); default 0</summary>
    public decimal SalvageValue { get; set; }

    /// <summary>อัตราค่าเสื่อมชุดบัญชี (% ต่อปี) — default จากมาสเตอร์, override ได้</summary>
    public decimal BookRatePct { get; set; }

    /// <summary>อัตราค่าเสื่อมชุดภาษี (% ต่อปี)</summary>
    public decimal TaxRatePct { get; set; }

    /// <summary>
    /// ค่าเสื่อมราคาสะสมยกมา ณ ต้นปี <see cref="BroughtForwardYear"/> (จาก Express FAMAS.ACCMBF).
    /// ถ้า &gt; 0 engine จะเริ่มสะสมจากยอดนี้ (แทนการคำนวณใหม่ทั้งหมด) เพื่อให้ตรง Express เป๊ะ.
    /// สินทรัพย์ที่ป้อนเอง (ไม่ได้ import) = 0 → engine คำนวณจากวันได้มา.
    /// </summary>
    public decimal AccumulatedBroughtForward { get; set; }

    /// <summary>ปีที่ <see cref="AccumulatedBroughtForward"/> เป็นยอดต้นปี (0 = ไม่มียอดยกมา)</summary>
    public int BroughtForwardYear { get; set; }

    /// <summary>รหัสกลุ่มสินทรัพย์จาก Express (FAMAS.FASGRP เช่น VE/EQ/TO) — เชิงอ้างอิง/จัดกลุ่ม</summary>
    public string? AssetGroupCode { get; set; }

    /// <summary>หมวดบัญชีจาก Express (FAMAS.ACCCOD เช่น VEH/EQU/TOL) — ใช้แมพ→บัญชี GL ต่อบริษัท</summary>
    public string? CategoryCode { get; set; }

    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.Active;

    // ── การจำหน่าย/ขาย (เมื่อ Status ≠ Active) ────────────────────────────────────
    public DateTime? DisposalDate { get; set; }

    /// <summary>ราคาขาย (เมื่อ Sold) — ใช้คำนวณกำไร/ขาดทุน = ราคาขาย − มูลค่าสุทธิ ณ วันขาย</summary>
    public decimal? DisposalProceeds { get; set; }

    public string? DisposalNote { get; set; }

    // ── ผูกบัญชี GL สำหรับ generate adjustment ────────────────────────────────────
    /// <summary>บัญชีสินทรัพย์ (ราคาทุน) — เชิงอ้างอิง</summary>
    public int? AssetAccountId { get; set; }

    /// <summary>บัญชีค่าเสื่อมราคาสะสม (contra-asset) — ปลายทาง Cr ตอนปรับปรุง</summary>
    public int AccumDepreciationAccountId { get; set; }

    /// <summary>บัญชีค่าเสื่อมราคา (P&amp;L) — ปลายทาง Dr ตอนปรับปรุง</summary>
    public int DepreciationExpenseAccountId { get; set; }

    public string? Notes { get; set; }
    public string? AttachmentPath { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// true = สินทรัพย์มาจากการนำเข้า Express FAMAS → ฟิลด์ที่ Express เป็นเจ้าของ
    /// (รหัส/ชื่อ/ราคาทุน/วันได้มา/มูลค่าซาก/ยอดยกมา/หมวด) เป็น read-only แก้ผ่าน CRUD ไม่ได้
    /// (ปรับที่ Express แล้ว re-import). false = ป้อนเอง (FAMAS ว่าง) → แก้ได้ทุกฟิลด์.
    /// ฟิลด์ที่ app เป็นเจ้าของ (อัตราบัญชี/ภาษี, บัญชี GL, สถานะ/จำหน่าย, หมายเหตุ) แก้ได้เสมอ.
    /// </summary>
    public bool IsFromExpress { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
    public AssetTypeMaster? AssetType { get; set; }
}
