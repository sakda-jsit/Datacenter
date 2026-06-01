namespace Datacenter.Application.Common.Security;

/// <summary>
/// บริการกลางสำหรับบังคับใช้ multi-company isolation
/// รวมตรรกะตรวจสิทธิ์การเข้าถึงบริษัทไว้ที่เดียว แทนที่จะให้แต่ละ handler ทำเอง
/// </summary>
public interface ICompanyAccessGuard
{
    /// <summary>
    /// ตรวจว่าผู้ใช้ปัจจุบันมีสิทธิ์เข้าถึงบริษัทรหัสที่ระบุหรือไม่
    /// Admin เข้าถึงได้ทุกบริษัท ผู้ใช้อื่นต้องมีสิทธิ์ใน CompanyUserAccess
    /// โยน <see cref="Exceptions.ForbiddenException"/> เมื่อไม่มีสิทธิ์
    /// </summary>
    Task EnsureAccessAsync(int clientCompanyId, CancellationToken ct = default);

    /// <summary>
    /// คืนรายการ ClientCompanyId ที่ผู้ใช้ปัจจุบันเข้าถึงได้
    /// คืน <c>null</c> เมื่อเป็น Admin (หมายถึงเข้าถึงได้ทุกบริษัท)
    /// ใช้สำหรับ endpoint แบบ list ที่ต้องกรองหลายบริษัทพร้อมกัน
    /// </summary>
    Task<IReadOnlyList<int>?> GetAccessibleCompanyIdsAsync(CancellationToken ct = default);
}
