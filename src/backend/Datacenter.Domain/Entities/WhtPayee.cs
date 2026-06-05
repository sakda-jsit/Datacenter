using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ข้อมูลติดต่อผู้ถูกหักภาษี ณ ที่จ่าย (ต่อบริษัทลูกค้า, key = เลขผู้เสียภาษี).
/// แยกจาก <see cref="WhtEntry"/> เพื่อให้ "อีเมล" ที่เจ้าหน้าที่กรอกไม่ถูกล้างตอน re-import ISTAX.
/// </summary>
public class WhtPayee : BaseEntity
{
    public int ClientCompanyId { get; set; }

    /// <summary>เลขประจำตัวผู้เสียภาษี/บัตรประชาชนของผู้ถูกหัก (business key)</summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>ชื่อผู้ถูกหัก (sync จาก ISTAX ล่าสุด)</summary>
    public string? Name { get; set; }

    /// <summary>อีเมลสำหรับส่งหนังสือรับรองหัก ณ ที่จ่าย (เจ้าหน้าที่กรอกเอง — import ไม่ทับ)</summary>
    public string? Email { get; set; }

    public ClientCompany ClientCompany { get; set; } = null!;
}
