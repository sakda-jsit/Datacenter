namespace Datacenter.Application.Common.Interfaces;

/// <summary>หนึ่งบรรทัดที่ parse ได้จาก statement (ก่อนบันทึก)</summary>
public record ParsedStatementLine(
    DateTime Date,
    string? Description,
    decimal Withdrawal,
    decimal Deposit,
    decimal? Balance);

/// <summary>
/// ผลการ parse statement: ระบุธนาคารที่ detect ได้, งวด, ยอดต้น/ปลาย, บรรทัด,
/// และผล balance self-check (ยอดต้น + Σฝาก − Σถอน = ยอดปลาย) เป็นตัวพิสูจน์ความถูกต้อง.
/// </summary>
public record BankStatementParseResult(
    string BankCode,
    string? AccountNo,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal ComputedClosing,
    bool BalanceCheckPasses,
    IReadOnlyList<ParsedStatementLine> Lines,
    string? Warning);

/// <summary>
/// แปลงไฟล์ statement (PDF ของ SCB/KBANK/TTB ด้วย coordinate-based parser, หรือ Excel/CSV เทมเพลต)
/// เป็นบรรทัดมาตรฐานเพื่อกระทบยอด. คืน BankCode="UNKNOWN" ถ้า PDF ไม่รองรับ.
/// </summary>
public interface IBankStatementParser
{
    BankStatementParseResult Parse(byte[] content, string fileName);

    /// <summary>สร้างเทมเพลต Excel (.xlsx) สำหรับกรอก statement: วันที่/รายละเอียด/ถอน/ฝาก/ยอดคงเหลือ</summary>
    byte[] BuildTemplate();
}
