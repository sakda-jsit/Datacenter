using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// สินค้าคงคลัง (ยอดคงเหลือ ณ ปัจจุบัน) — นำเข้าจาก Express STMAS.DBF (upsert by รหัสสินค้า).
/// ใช้รายงานมูลค่าสินค้าคงเหลือ + เทียบกับบัญชีสินค้าคงเหลือใน GL (FG↔TB) เพื่อแสดงผลต่างให้ปรับปรุงเอง.
/// </summary>
public class StockItem : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสสินค้า (STMAS.STKCOD) — business key</summary>
    public string StockCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;   // STKDES
    public string? ItemType { get; set; }               // STKTYP (รหัสประเภท)
    public string? GroupCode { get; set; }              // STKGRP
    public string? CategoryCode { get; set; }           // ACCCOD (หมวดบัญชีสินค้า)
    public string? Unit { get; set; }                   // QUCOD (หน่วยนับ)

    /// <summary>จำนวนคงเหลือ (STMAS.TOTBAL)</summary>
    public decimal OnHandQty { get; set; }

    /// <summary>ราคาทุนต่อหน่วย (STMAS.UNITPR)</summary>
    public decimal UnitCost { get; set; }

    /// <summary>มูลค่าคงเหลือ (STMAS.TOTVAL)</summary>
    public decimal StockValue { get; set; }

    public bool IsActive { get; set; } = true;          // STATUS != '0'
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
