using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

/// <summary>
/// ทะเบียนผู้ทำบัญชี — master ของสำนักงาน ใช้ซ้ำได้หลายบริษัท/ปี (ผู้ทำบัญชี 1 คนทำได้หลายราย).
/// สำนักงานทำบัญชีเก็บใน OfficeProfile (สำนักงานของผู้ใช้เอง). ป้อนมือ — แก้ไข/ลบได้ + audit.
/// </summary>
public class Bookkeeper : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>เลขประจำตัวผู้เสียภาษีอากร (13 หลัก) ของผู้ทำบัญชี</summary>
    public string? TaxId { get; set; }

    public bool IsActive { get; set; } = true;
}
