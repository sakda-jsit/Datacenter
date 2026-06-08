namespace Datacenter.Application.Features.Bank.Services;

/// <summary>
/// เทียบเลขบัญชีที่อ่านได้จาก statement กับเลขบัญชีของบริษัทใน Express (BankAccount.AccountNumber)
/// โดยตัดอักขระที่ไม่ใช่ตัวเลขออกก่อน (รูปแบบ xxx-x-xxxxx-x vs xxxxxxxxxx).
/// รองรับเลขบัญชีที่ถูกปิดบังบางส่วนใน statement ด้วยการเทียบส่วนท้าย (suffix) อย่างน้อย 4 หลัก.
/// </summary>
public static class BankAccountNumberComparer
{
    /// <summary>เหลือเฉพาะตัวเลข</summary>
    public static string DigitsOnly(string? s)
        => string.IsNullOrEmpty(s) ? string.Empty : new string(s.Where(char.IsDigit).ToArray());

    /// <returns>
    /// true = ตรงกัน, false = ไม่ตรง (ควรแจ้งเตือน),
    /// null = ระบุไม่ได้ (ฝั่งใดฝั่งหนึ่งไม่มีเลขบัญชีให้เทียบ)
    /// </returns>
    public static bool? Matches(string? statementAccountNo, string? companyAccountNo)
    {
        var a = DigitsOnly(statementAccountNo);
        var b = DigitsOnly(companyAccountNo);
        if (a.Length == 0 || b.Length == 0) return null; // ไม่มีข้อมูลให้เทียบ

        if (a == b) return true;

        // รองรับเลขบัญชีที่ปิดบังบางส่วน / รูปแบบต่างกัน → เทียบส่วนท้าย ≥ 4 หลัก
        int n = Math.Min(a.Length, b.Length);
        if (n >= 4 && (a.EndsWith(b) || b.EndsWith(a))) return true;

        return false;
    }
}
