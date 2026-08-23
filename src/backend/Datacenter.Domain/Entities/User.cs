using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>อีเมล (สำหรับแจ้งเตือนงานที่มอบหมาย) — optional</summary>
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>true = ต้องเปลี่ยนรหัสผ่านก่อนใช้งาน (ผู้ใช้ใหม่ / admin รีเซ็ตรหัสให้)</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>จำนวนครั้งที่ใส่รหัสผิดต่อเนื่อง — ครบเกณฑ์แล้วล็อกชั่วคราว (กัน brute force)</summary>
    public int FailedLoginCount { get; set; }

    /// <summary>ล็อกบัญชีถึงเวลานี้ (UTC); null = ไม่ถูกล็อก</summary>
    public DateTime? LockedUntil { get; set; }

    public ICollection<CompanyUserAccess> CompanyAccesses { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
