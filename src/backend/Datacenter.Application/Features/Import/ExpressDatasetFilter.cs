using Datacenter.Application.Features.Import.DTOs;

namespace Datacenter.Application.Features.Import;

/// <summary>
/// กฎทางธุรกิจสำหรับคัดเฉพาะบริษัทปัจจุบันจากทะเบียนข้อมูล Express (sccomp.dbf)
/// ตัดออก:
///   1. ชื่อข้อมูลที่ขึ้นต้นด้วย "X-" (ข้อมูลปีเก่า), "Z-" (สำเนา/ทดสอบ) หรือ "A-" (เลิกใช้/สำรอง)
///   2. รายการที่คอลัมน์ CANDEL = "N"
///   3. โฟลเดอร์ที่ขึ้นต้นด้วย COPY / X- / Z- / A-
/// เจ้าหน้าที่บัญชีทำเครื่องหมายเลิกใช้ด้วยการ "เปลี่ยนชื่อข้อมูล" ใน Express เท่านั้น
/// ทะเบียนจึงเป็นแหล่งความจริง — ถ้าบริษัทไหนยังใช้งานอยู่ ต้องเอา prefix ออกที่ Express
/// หมายเหตุ: เทียบ prefix แบบขีดกลางเท่านั้น (ขีดล่าง เช่น "X_ABC" ถือเป็นบริษัทปัจจุบัน)
/// </summary>
public static class ExpressDatasetFilter
{
    /// <summary>prefix ที่ Express ใช้ทำเครื่องหมายว่าชุดข้อมูลนี้ไม่ใช่บริษัทที่ทำบัญชีอยู่</summary>
    private static readonly string[] ArchivedPrefixes = ["X-", "Z-", "A-"];

    public static bool IsCurrentCompany(ExpressDatasetDto dataset)
    {
        var name = dataset.CompName.TrimStart();
        var path = dataset.Path.TrimStart();

        if (StartsWithAny(name, ArchivedPrefixes)) return false;
        if (string.Equals(dataset.Candel.Trim(), "N", StringComparison.OrdinalIgnoreCase)) return false;

        // โฟลเดอร์/รหัสที่เป็นสำเนา-ทดสอบ — ตัดออกแม้ชื่อข้อมูลไม่ได้ขึ้นต้นด้วย prefix เหล่านี้
        if (path.StartsWith("COPY", StringComparison.OrdinalIgnoreCase)) return false;
        if (StartsWithAny(path, ArchivedPrefixes)) return false;

        return true;
    }

    private static bool StartsWithAny(string value, string[] prefixes)
        => prefixes.Any(p => value.StartsWith(p, StringComparison.OrdinalIgnoreCase));
}
