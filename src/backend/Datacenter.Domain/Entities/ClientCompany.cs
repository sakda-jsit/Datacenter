using Datacenter.Domain.Common;

namespace Datacenter.Domain.Entities;

public class ClientCompany : BaseEntity
{
    /// <summary>รหัส dataset ใน Express (เช่น JSIT2016) — ใช้หาโฟลเดอร์ DBF, ไม่แสดงในระบบ</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>ชื่อจาก Express (THINAM) — sync ทับได้ทุกครั้งที่ import เป็นค่าอ้างอิง</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ชื่อทางการสำหรับออกงบ/แสดงทั้งระบบ. import/seed ครั้งแรก default = ชื่อ Express
    /// แล้วแก้ไขได้เอง; import ครั้งถัดไป **ไม่ทับ** ค่านี้ (กันชื่อที่แก้แล้วหาย).
    /// (Historical: เมื่อทำ report package จะ snapshot ค่านี้ต่อปี/version — docs/18)
    /// </summary>
    public string LegalName { get; set; } = string.Empty;

    /// <summary>ชื่ออังกฤษจาก Express (ENGNAM) — sync ได้</summary>
    public string? EnglishName { get; set; }

    public string TaxId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = "00000";
    /// <summary>ที่อยู่รวมบรรทัดเดียว (จาก Express ISINFO) — sync ได้; ใช้เป็นแหล่งแยกช่อง Addr* ตอน import</summary>
    public string? Address { get; set; }

    // ── ที่อยู่แยกช่อง (สำหรับออกแบบฟอร์มราชการ เช่น ภ.ง.ด.50) — เติมจาก Address ตอน import ถ้าว่าง, แก้ได้เอง ──
    public string? AddrBuilding { get; set; }       // อาคาร
    public string? AddrRoomNo { get; set; }         // ห้องเลขที่
    public string? AddrFloor { get; set; }          // ชั้นที่
    public string? AddrVillage { get; set; }        // หมู่บ้าน
    public string? AddrHouseNo { get; set; }        // เลขที่
    public string? AddrMoo { get; set; }            // หมู่ที่
    public string? AddrSoi { get; set; }            // ตรอก/ซอย
    public string? AddrRoad { get; set; }           // ถนน
    public string? AddrSubDistrict { get; set; }    // ตำบล/แขวง
    public string? AddrDistrict { get; set; }       // อำเภอ/เขต
    public string? AddrProvince { get; set; }       // จังหวัด
    // หมายเหตุ: รหัสไปรษณีย์ใช้ฟิลด์ PostalCode เดิมด้านล่าง

    public int FiscalYearStartMonth { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // ── ข้อมูลประกันสังคม (นายจ้าง) สำหรับ สปส.1-10 ──
    /// <summary>เลขที่บัญชีนายจ้าง ปกส. (10 หลัก เช่น 2000398553)</summary>
    public string? SsoAccountNo { get; set; }
    /// <summary>ลำดับที่สาขา ปกส. (6 หลัก, สำนักงานใหญ่ = 000000)</summary>
    public string? SsoBranchCode { get; set; }
    public string? Phone { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>รูปลายเซ็นผู้มีอำนาจ (PNG/JPG) — ใช้แนบในหนังสือรับรองหัก ณ ที่จ่าย ฯลฯ</summary>
    public byte[]? SignatureImage { get; set; }

    public ICollection<CompanyUserAccess> UserAccesses { get; set; } = [];
}
