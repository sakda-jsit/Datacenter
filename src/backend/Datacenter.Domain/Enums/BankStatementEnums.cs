namespace Datacenter.Domain.Enums;

/// <summary>สถานะรอบกระทบยอด statement (เก็บเป็น int ตาม convention โปรเจกต์)</summary>
public enum BankStatementImportStatus
{
    Draft = 0,
    Reconciled = 1
}

/// <summary>สถานะการจับคู่ของบรรทัด statement กับรายการในสมุด (BankTransaction)</summary>
public enum BankLineMatchStatus
{
    /// <summary>ยังไม่จับคู่</summary>
    Unmatched = 0,
    /// <summary>ระบบจับคู่อัตโนมัติ (วันที่+จำนวน+ทิศทาง)</summary>
    AutoMatched = 1,
    /// <summary>ผู้ใช้จับคู่เอง</summary>
    ManualMatched = 2,
    /// <summary>ข้าม (ไม่ต้องจับคู่ เช่น ยกยอด)</summary>
    Ignored = 3
}
