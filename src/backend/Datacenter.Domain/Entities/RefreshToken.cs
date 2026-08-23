using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// refresh token สำหรับต่ออายุการเข้าใช้งานโดยไม่ต้อง login ใหม่.
/// เก็บเฉพาะ <b>hash</b> ของ token (ค่าจริงอยู่ที่ client เท่านั้น) — ฐานข้อมูลรั่วก็สวมสิทธิ์ไม่ได้.
/// ใช้แบบ rotation: ขอ token ใหม่ = revoke ใบเดิมทันที (ใบเดิมถูกใช้ซ้ำ = ถือว่ารั่ว → revoke ทั้งสาย).
/// </summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    /// <summary>เหตุผลที่ถูกยกเลิก (rotated / logout / password-changed / reused)</summary>
    public string? RevokedReason { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
