using Datacenter.Application.Common.Exceptions;

namespace Datacenter.Application.Common.Security;

/// <summary>
/// เกณฑ์รหัสผ่านขั้นต่ำของระบบ — ใช้ร่วมกันทุกที่ที่ตั้ง/เปลี่ยนรหัส (สร้างผู้ใช้ / รีเซ็ต / เปลี่ยนเอง).
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>รหัสที่ห้ามใช้ (รวมรหัสเริ่มต้นของระบบ เพื่อบังคับให้เปลี่ยนจริง)</summary>
    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin1234", "password", "password1", "12345678", "123456789", "qwerty123", "abc12345",
    };

    /// <summary>คืนข้อความปัญหา (null = ผ่าน)</summary>
    public static string? Check(string? password, string? username)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "กรุณากรอกรหัสผ่าน";
        if (password.Length < MinLength)
            return $"รหัสผ่านต้องมีอย่างน้อย {MinLength} ตัวอักษร";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "รหัสผ่านต้องมีทั้งตัวอักษรและตัวเลข";
        if (Forbidden.Contains(password))
            return "รหัสผ่านนี้ใช้กันทั่วไป/เป็นรหัสเริ่มต้นของระบบ — กรุณาตั้งรหัสอื่น";
        if (!string.IsNullOrWhiteSpace(username) && password.Equals(username, StringComparison.OrdinalIgnoreCase))
            return "รหัสผ่านต้องไม่ซ้ำกับชื่อผู้ใช้";
        return null;
    }

    /// <summary>ตรวจแล้ว throw ValidationException (แปลงเป็น 422 โดย middleware) ถ้าไม่ผ่าน</summary>
    public static void EnsureValid(string? password, string? username, string field = "password")
    {
        var problem = Check(password, username);
        if (problem is not null)
            throw new ValidationException(new Dictionary<string, string[]> { [field] = [problem] });
    }
}
