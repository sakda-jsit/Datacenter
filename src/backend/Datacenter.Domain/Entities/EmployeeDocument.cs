using Datacenter.Domain.Common;
using Datacenter.Domain.Enums;

namespace Datacenter.Domain.Entities;

/// <summary>
/// คลังหลักฐาน/เอกสารของพนักงาน — รูปหน้าบัตร ปชช., หลักฐานแจ้งเข้า-ออก ปกส., สลิป ฯลฯ.
/// PDPA: เข้าถึงแบบควบคุมสิทธิ์ + audit ทุกการดู/ดาวน์โหลด (เก็บไฟล์เป็น blob ใน DB).
/// </summary>
public class EmployeeDocument : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public EmployeeDocType DocType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Content { get; set; } = [];

    /// <summary>วันที่มีผล (เช่น วันที่แจ้ง ปกส.) — ใช้กับหลักฐานแจ้งเข้า-ออก</summary>
    public DateTime? EffectiveDate { get; set; }
    public string? Note { get; set; }
}
