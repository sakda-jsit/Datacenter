using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รายการเดินบัญชีธนาคาร (สมุดเงินฝาก) — นำเข้าจาก Express BKTRN.DBF.
/// ทิศทาง: <see cref="IsDeposit"/> = true เมื่อ BKTRN.JNLTRNTYP='0' (เงินเข้า), false เมื่อ '1' (เงินออก).
/// transactional → import แทนที่ทั้งชุดต่อบริษัท.
/// </summary>
public class BankTransaction : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>รหัสบัญชีธนาคาร (BKTRN.BNKACC) → โยงกับ BankAccount</summary>
    public string BankAccountCode { get; set; } = string.Empty;

    public DateTime TransactionDate { get; set; }       // TRNDAT
    public string? TransactionType { get; set; }        // BKTRNTYP (bP/BD/BW/Bx/BT/bR)

    /// <summary>true = เงินเข้า (ฝาก/รับ/โอนเข้า), false = เงินออก (จ่าย/ถอน/โอนออก)</summary>
    public bool IsDeposit { get; set; }

    public string? ChequeNo { get; set; }               // CHQNUM
    public DateTime? ChequeDate { get; set; }           // CHQDAT
    public string? CounterpartyName { get; set; }       // NAME
    public decimal Amount { get; set; }                 // NETAMT (จำนวนเงินรายการ)
    public decimal Charge { get; set; }                 // CHARGE (ค่าธรรมเนียม)
    public string? Remark { get; set; }                 // REMARK
    public string? Voucher { get; set; }                // VOUCHER
    public string? ChequeStatus { get; set; }           // CHQSTAT
    public int? ImportBatchId { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
