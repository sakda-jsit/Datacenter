using Datacenter.Domain.Enums;

namespace Datacenter.Api;

/// <summary>
/// ชุดบทบาทที่ใช้กับ <c>[Authorize(Roles = ...)]</c> — รวมไว้ที่เดียวกันลืมแก้ไม่ครบ
/// </summary>
public static class AuthRoles
{
    /// <summary>ผู้ดูแลระบบเท่านั้น — ใช้กับข้อมูลกำกับดูแล เช่น ประวัติการใช้งาน</summary>
    public const string AdminOnly = nameof(UserRole.Admin);

    /// <summary>
    /// ตั้งค่ากลางของสำนักงาน (ผู้ใช้งานระบบ, โปรไฟล์สำนักงาน, ทะเบียนผู้สอบ/ผู้ทำบัญชี,
    /// มอบหมายผู้ลงนาม, อัตราเงินสมทบ) — Admin และหัวหน้างาน
    /// </summary>
    public const string CentralSettings = nameof(UserRole.Admin) + "," + nameof(UserRole.Supervisor);
}
