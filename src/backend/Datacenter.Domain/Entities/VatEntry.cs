using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// หนึ่งรายการในรายงานภาษีมูลค่าเพิ่ม (ภาษีซื้อ/ภาษีขาย) — นำเข้าจาก Express ISVAT.DBF 100%.
/// ใช้ออกรายงาน ภ.พ.30 รายเดือน (สรุปตาม <see cref="TaxPeriod"/>) และรายละเอียดรายใบกำกับภาษี.
/// เป็นข้อมูล transactional (ไม่แก้มือ) → import แทนที่ทั้งชุดต่อบริษัท (sync จาก Express).
/// </summary>
public class VatEntry : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>ภาษีขาย (Output) หรือ ภาษีซื้อ (Input)</summary>
    public VatEntryType VatType { get; set; }

    /// <summary>เดือนภาษี (วันแรกของเดือน) จาก ISVAT.VATPRD — ใช้จัดกลุ่ม ภ.พ.30</summary>
    public DateTime TaxPeriod { get; set; }

    /// <summary>วันที่ในใบกำกับภาษี (ISVAT.DOCDAT)</summary>
    public DateTime? DocumentDate { get; set; }

    /// <summary>วันที่ลงรายการภาษี (ISVAT.VATDAT)</summary>
    public DateTime? VatDate { get; set; }

    /// <summary>เลขที่ใบกำกับภาษี (ISVAT.DOCNUM)</summary>
    public string DocumentNo { get; set; } = string.Empty;

    /// <summary>เลขที่อ้างอิง (ISVAT.REFNUM)</summary>
    public string? ReferenceNo { get; set; }

    /// <summary>ชื่อคู่ค้า/รายละเอียด (ISVAT.DESCRP)</summary>
    public string? Description { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษีของคู่ค้า (ISVAT.TAXID)</summary>
    public string? CounterpartyTaxId { get; set; }

    /// <summary>คำนำหน้าชื่อคู่ค้า (ISVAT.PRENAM เช่น บจ./หจก.)</summary>
    public string? CounterpartyPrefix { get; set; }

    /// <summary>มูลค่าสินค้า/บริการ (ฐานภาษี) = ISVAT.AMT01+AMT02</summary>
    public decimal BaseAmount { get; set; }

    /// <summary>จำนวนภาษีมูลค่าเพิ่ม = ISVAT.VAT01+VAT02</summary>
    public decimal VatAmount { get; set; }

    /// <summary>มูลค่าอัตรา 0% (ส่งออก/ยกเว้น) = ISVAT.AMTRAT0</summary>
    public decimal ZeroRatedAmount { get; set; }

    /// <summary>ยื่นล่าช้า (ISVAT.LATE='Y')</summary>
    public bool IsLate { get; set; }

    /// <summary>ประเภทเอกสารดิบจาก Express (ISVAT.RECTYP) — เชิงอ้างอิง</summary>
    public string? RecordType { get; set; }

    /// <summary>batch ที่นำเข้ารายการนี้</summary>
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
