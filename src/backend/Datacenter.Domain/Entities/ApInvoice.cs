using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ใบตั้งหนี้/ใบกำกับซื้อ (เจ้าหนี้การค้า) — นำเข้าจาก Express APTRN.DBF (เฉพาะ RECTYP='3' = ใบรับ RR).
/// transactional → import แทนที่ทั้งชุดต่อบริษัท. ยอดค้างชำระ = <see cref="OutstandingAmount"/> (APTRN.REMAMT).
/// </summary>
public class ApInvoice : BaseEntity
{
    public int ClientCompanyId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;   // DOCNUM (RR...)
    public DateTime DocumentDate { get; set; }               // DOCDAT
    public DateTime? DueDate { get; set; }                   // DUEDAT

    public string SupplierCode { get; set; } = string.Empty; // SUPCOD
    public string? SupplierName { get; set; }                // denormalized (จาก APMAS)

    public decimal Amount { get; set; }            // AFTDISC
    public decimal VatRate { get; set; }           // VATRAT
    public decimal VatAmount { get; set; }         // VATAMT
    public decimal NetAmount { get; set; }         // NETAMT (รวม VAT)
    public decimal PaidAmount { get; set; }        // PAYAMT (จ่ายชำระแล้ว)
    public decimal OutstandingAmount { get; set; } // REMAMT (คงค้าง)

    /// <summary>ชำระครบแล้ว (APTRN.CMPLAPP='Y')</summary>
    public bool IsCompleted { get; set; }

    public DateTime? VatPeriod { get; set; }       // VATPRD
    public string? Reference { get; set; }         // YOUREF/REFNUM
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
