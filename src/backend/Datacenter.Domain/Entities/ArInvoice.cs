using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ใบแจ้งหนี้ลูกหนี้การค้า — นำเข้าจาก Express ARTRN.DBF (เฉพาะ RECTYP='3' = ใบแจ้งหนี้ IV).
/// transactional (ดึงจาก Express 100%) → import แทนที่ทั้งชุดต่อบริษัท.
/// ยอดค้างชำระ = <see cref="OutstandingAmount"/> (ARTRN.REMAMT).
/// </summary>
public class ArInvoice : BaseEntity
{
    public int ClientCompanyId { get; set; }

    public string DocumentNo { get; set; } = string.Empty;   // DOCNUM (IV...)
    public DateTime DocumentDate { get; set; }               // DOCDAT
    public DateTime? DueDate { get; set; }                   // DUEDAT

    public string CustomerCode { get; set; } = string.Empty; // CUSCOD
    public string? CustomerName { get; set; }                // denormalized (จาก ARMAS)

    public decimal Amount { get; set; }        // AFTDISC (มูลค่าก่อน VAT หลังหักส่วนลด)
    public decimal VatRate { get; set; }       // VATRAT
    public decimal VatAmount { get; set; }     // VATAMT
    public decimal NetAmount { get; set; }     // NETAMT (รวม VAT)
    public decimal ReceivedAmount { get; set; }    // RCVAMT (รับชำระแล้ว)
    public decimal OutstandingAmount { get; set; } // REMAMT (คงค้าง)

    /// <summary>ชำระครบแล้ว (ARTRN.CMPLAPP='Y')</summary>
    public bool IsCompleted { get; set; }

    public DateTime? VatPeriod { get; set; }   // VATPRD
    public string? Reference { get; set; }     // YOUREF
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
