namespace Datacenter.Application.Common.Security;

/// <summary>
/// บริการกลางสำหรับบังคับใช้สิทธิ์รายบริษัท รวมตรรกะไว้ที่เดียวแทนที่จะให้แต่ละ handler ทำเอง
///
/// สิทธิ์มี 2 ระดับ:
/// <list type="bullet">
/// <item><b>ดู (read)</b> — ผู้ใช้ที่เข้าระบบได้ ดูข้อมูลบัญชี/ภาษี/งบ ของ <b>ทุกบริษัท</b>
/// เพื่อให้ช่วยงานกันข้ามบริษัทได้</item>
/// <item><b>ผู้ดูแล (owner)</b> — ต้องมีแถวใน CompanyUserAccess ของบริษัทนั้น จึงจะ
/// <b>ทำรายการ</b> (บันทึก/แก้/ลบ/นำเข้า/ปิดงวด) และ <b>ดูข้อมูลเงินเดือน</b> (PDPA) ได้</item>
/// </list>
/// Admin ผ่านทั้งสองระดับเสมอ
/// </summary>
public interface ICompanyAccessGuard
{
    /// <summary>
    /// ตรวจสิทธิ์ระดับ "ดู" — ผู้ใช้ที่เข้าระบบแล้วดูได้ทุกบริษัท
    /// โยน <see cref="Exceptions.ForbiddenException"/> เมื่อไม่พบผู้ใช้ปัจจุบัน
    /// </summary>
    Task EnsureAccessAsync(int clientCompanyId, CancellationToken ct = default);

    /// <summary>
    /// ตรวจสิทธิ์ระดับ "ผู้ดูแล" — ต้องเป็น Admin หรือมีสิทธิ์ดูแลบริษัทนั้นใน CompanyUserAccess
    /// ใช้กับทุก command และ query ของโมดูลเงินเดือน
    /// โยน <see cref="Exceptions.ForbiddenException"/> เมื่อไม่ใช่ผู้ดูแล
    /// </summary>
    Task EnsureOwnerAccessAsync(int clientCompanyId, CancellationToken ct = default);

    /// <summary>
    /// คืนรายการ ClientCompanyId ที่ผู้ใช้ปัจจุบัน <b>ดู</b> ได้ — คืน <c>null</c> = ทุกบริษัท
    /// (ปัจจุบันคืน null เสมอ เพราะทุกคนดูได้ทุกบริษัท แต่คงเมธอดไว้เพื่อให้ handler ที่ทำ
    /// aggregate หลายบริษัทไม่ต้องแก้ ถ้าอนาคตเปลี่ยนนโยบาย)
    /// </summary>
    Task<IReadOnlyList<int>?> GetAccessibleCompanyIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// คืนรายการ ClientCompanyId ที่ผู้ใช้ปัจจุบัน <b>ดูแล</b> (ทำรายการได้)
    /// คืน <c>null</c> เมื่อเป็น Admin (ดูแลได้ทุกบริษัท)
    /// </summary>
    Task<IReadOnlyList<int>?> GetOwnedCompanyIdsAsync(CancellationToken ct = default);
}
