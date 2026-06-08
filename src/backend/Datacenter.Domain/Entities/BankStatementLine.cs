using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// หนึ่งบรรทัดรายการใน bank statement ที่นำเข้ามากระทบยอด.
/// ทิศทางแยกเป็น Withdrawal (เงินออก) / Deposit (เงินเข้า) — ระบุจาก balance-delta ตอน parse.
/// </summary>
public class BankStatementLine : BaseEntity
{
    public int BankStatementImportId { get; set; }

    public DateTime LineDate { get; set; }
    public string? Description { get; set; }

    /// <summary>เงินออก (ถอน/จ่าย)</summary>
    public decimal Withdrawal { get; set; }

    /// <summary>เงินเข้า (ฝาก/รับ)</summary>
    public decimal Deposit { get; set; }

    /// <summary>ยอดคงเหลือหลังรายการ (จาก statement, ถ้ามี)</summary>
    public decimal? Balance { get; set; }

    /// <summary>รายการในสมุด (BankTransaction) ที่จับคู่ได้</summary>
    public int? MatchedBankTransactionId { get; set; }

    public BankLineMatchStatus MatchStatus { get; set; } = BankLineMatchStatus.Unmatched;

    public BankStatementImport Import { get; set; } = null!;
}
