namespace Datacenter.Application.Common.Security;

/// <summary>
/// ค่าตั้งความปลอดภัยการเข้าสู่ระบบ (section "Auth" ใน appsettings / environment variable).
/// ค่าเริ่มต้นเป็นค่าที่ปลอดภัยพอสำหรับใช้งานจริง — ปรับได้ตอน deploy โดยไม่ต้องแก้โค้ด.
/// </summary>
public class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>อายุ access token (นาที) — สั้นเพื่อลดผลกระทบเมื่อ token รั่ว (ต่ออายุด้วย refresh token)</summary>
    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>อายุ refresh token (วัน) — ผู้ใช้ไม่ต้อง login ใหม่ภายในช่วงนี้</summary>
    public int RefreshTokenDays { get; set; } = 14;

    /// <summary>ใส่รหัสผิดต่อเนื่องกี่ครั้งจึงล็อกบัญชีชั่วคราว</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>ล็อกบัญชีนานกี่นาทีเมื่อใส่รหัสผิดครบเกณฑ์</summary>
    public int LockoutMinutes { get; set; } = 15;
}
