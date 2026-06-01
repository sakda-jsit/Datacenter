using Datacenter.Application.Features.Import.DTOs;

namespace Datacenter.Application.Features.Import;

/// <summary>
/// กฎทางธุรกิจสำหรับคัดเฉพาะบริษัทปัจจุบันจากทะเบียนข้อมูล Express (sccomp.dbf)
/// ตัดออก:
///   1. ชื่อข้อมูลที่ขึ้นต้นด้วย "X-" (ข้อมูลปีเก่า) หรือ "Z-" (สำเนา/ทดสอบ)
///   2. รายการที่คอลัมน์ CANDEL = "N"
/// หมายเหตุ: เทียบ prefix แบบขีดกลางเท่านั้น เช่น "X_SIAMCM" (ขีดล่าง) ถือเป็นบริษัทปัจจุบัน
/// </summary>
public static class ExpressDatasetFilter
{
    public static bool IsCurrentCompany(ExpressDatasetDto dataset)
    {
        var name = dataset.CompName.TrimStart();

        if (name.StartsWith("X-", StringComparison.OrdinalIgnoreCase)) return false;
        if (name.StartsWith("Z-", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(dataset.Candel.Trim(), "N", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }
}
