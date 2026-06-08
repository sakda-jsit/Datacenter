using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// รอบการนำเข้า bank statement เพื่อกระทบยอด (RPT-015/016, docs/17).
/// รับเข้าจากไฟล์ PDF (auto-detect SCB/KBANK/TTB) หรือ Excel/CSV เทมเพลต หรือกรอกมือ
/// → จับคู่กับรายการในสมุด (BankTransaction จาก BKTRN). เก็บไฟล์ต้นฉบับเป็น blob.
/// </summary>
public class BankStatementImport : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>บัญชีธนาคารในระบบที่นำมากระทบยอด (BankAccount.Id)</summary>
    public int BankAccountId { get; set; }

    /// <summary>รหัสธนาคารที่ parse ได้ (SCB/KBANK/TTB) หรือ MANUAL/EXCEL</summary>
    public string BankCode { get; set; } = string.Empty;

    /// <summary>เลขที่บัญชีตาม statement (อ้างอิง)</summary>
    public string? StatementAccountNo { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>ยอดยกมาต้นงวดตาม statement</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>ยอดคงเหลือปลายงวดตาม statement</summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>balance self-check ผ่าน (ยอดต้น + Σฝาก − Σถอน = ยอดปลาย)</summary>
    public bool ParsedOk { get; set; }

    // ── ไฟล์ต้นฉบับ (evidence) ──────────────────────────────────────────────
    public string? SourceFileName { get; set; }
    public byte[]? SourceContent { get; set; }
    public string? Sha256 { get; set; }
    public long ByteSize { get; set; }

    public string? Note { get; set; }
    public BankStatementImportStatus Status { get; set; } = BankStatementImportStatus.Draft;

    public ClientCompany ClientCompany { get; set; } = null!;
    public ICollection<BankStatementLine> Lines { get; set; } = new List<BankStatementLine>();
}
