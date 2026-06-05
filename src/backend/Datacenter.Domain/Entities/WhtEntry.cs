using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// หนึ่งรายการภาษีหัก ณ ที่จ่าย — นำเข้าจาก Express ISTAX.DBF 100%.
/// ใช้ออกรายงาน ภ.ง.ด.3 (บุคคลธรรมดา) / ภ.ง.ด.53 (นิติบุคคล) รายเดือน + รายละเอียดรายผู้ถูกหัก.
/// transactional (ไม่แก้มือ) → import แทนที่ทั้งชุดต่อบริษัท (sync จาก Express).
/// </summary>
public class WhtEntry : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>
    /// คีย์อ้างอิงรายการต้นทางใน Express (TAXNUM/REFNUM/ลำดับ + line index) — unique ต่อบริษัท.
    /// ใช้ upsert ตอน re-import เพื่อให้ Id เสถียรและไม่ล้างสถานะส่งเมล.
    /// </summary>
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>ภ.ง.ด.3 หรือ ภ.ง.ด.53 (จาก ISTAX.TAXTYP S03/S53)</summary>
    public WhtFormType FormType { get; set; }

    /// <summary>เดือนภาษี (วันแรกของเดือน) จาก ISTAX.TAXPRD — ใช้จัดกลุ่มรายงาน</summary>
    public DateTime TaxPeriod { get; set; }

    /// <summary>วันที่หักภาษี (ISTAX.TAXDAT)</summary>
    public DateTime? WithholdDate { get; set; }

    /// <summary>เลขที่หนังสือรับรองการหัก (ISTAX.TAXNUM)</summary>
    public string DocumentNo { get; set; } = string.Empty;

    /// <summary>เลขที่อ้างอิง/เอกสารต้นทาง (ISTAX.REFNUM)</summary>
    public string? ReferenceNo { get; set; }

    /// <summary>ชื่อผู้ถูกหักภาษี (ISTAX.NAME)</summary>
    public string? PayeeName { get; set; }

    /// <summary>คำนำหน้าชื่อผู้ถูกหัก (ISTAX.PRENAM)</summary>
    public string? PayeePrefix { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษี/บัตรประชาชนของผู้ถูกหัก (ISTAX.TAXID)</summary>
    public string? PayeeTaxId { get; set; }

    /// <summary>ที่อยู่ผู้ถูกหัก (ISTAX.ADDR) — ใช้พิมพ์ในหนังสือรับรอง 50 ทวิ</summary>
    public string? PayeeAddress { get; set; }

    /// <summary>ประเภทเงินได้ (ISTAX.TAXDES เช่น ค่าจ้างทำของ/ค่าเช่า/ค่าบริการ)</summary>
    public string? IncomeType { get; set; }

    /// <summary>จำนวนเงินที่จ่าย (ฐานภาษี) = ISTAX.AMOUNT</summary>
    public decimal BaseAmount { get; set; }

    /// <summary>อัตราภาษีหัก ณ ที่จ่าย (%) = ISTAX.TAXRAT</summary>
    public decimal TaxRate { get; set; }

    /// <summary>ภาษีที่หักไว้ = ISTAX.TAXAMT (= BaseAmount × TaxRate%)</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>เงื่อนไขการหัก (ISTAX.TAXCOND; '1'=หัก ณ ที่จ่าย) — เชิงอ้างอิง</summary>
    public string? Condition { get; set; }

    /// <summary>ยื่นล่าช้า (ISTAX.LATE='Y')</summary>
    public bool IsLate { get; set; }

    /// <summary>batch ที่นำเข้ารายการนี้</summary>
    public int? ImportBatchId { get; set; }

    // ── สถานะการส่งหนังสือรับรองทางอีเมล (คงไว้ข้าม re-import) ──────────────────
    public WhtEmailStatus EmailStatus { get; set; } = WhtEmailStatus.NotSent;

    /// <summary>อีเมลที่ส่งไปจริง (snapshot ณ เวลาส่ง)</summary>
    public string? EmailRecipient { get; set; }

    public DateTime? EmailSentAt { get; set; }

    /// <summary>ผู้ใช้ที่กดส่ง</summary>
    public string? EmailSentBy { get; set; }

    /// <summary>ข้อความ error เมื่อส่งไม่สำเร็จ</summary>
    public string? EmailError { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
