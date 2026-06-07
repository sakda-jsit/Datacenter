namespace Datacenter.Application.Common.Auditing;

/// <summary>
/// คุมการ "ปิด" field-level audit interceptor ชั่วคราว สำหรับงานที่ไม่ใช่การแก้ไขโดยผู้ใช้
/// เช่น การ sync ข้อมูลจาก Express (import upsert) — ไม่ควรนับเป็น user edit.
/// ใช้ AsyncLocal เพื่อให้ครอบเฉพาะ flow ปัจจุบัน (ปลอดภัยต่อ concurrent request).
/// </summary>
public static class AuditScope
{
    private static readonly AsyncLocal<bool> _suppressed = new();

    /// <summary>true เมื่ออยู่ในขอบเขตที่สั่งปิด field-level audit</summary>
    public static bool IsSuppressed => _suppressed.Value;

    /// <summary>เปิดขอบเขตปิด audit — คืน IDisposable ไว้ปิดท้ายด้วย using.</summary>
    public static IDisposable Suppress()
    {
        var previous = _suppressed.Value;
        _suppressed.Value = true;
        return new Restorer(previous);
    }

    private sealed class Restorer(bool previous) : IDisposable
    {
        public void Dispose() => _suppressed.Value = previous;
    }
}
