namespace Datacenter.Domain.Enums;

/// <summary>ประเภทการจ้าง/จ่ายค่าตอบแทน</summary>
public enum SalaryType
{
    Monthly = 1,   // รายเดือน
    Daily = 2,     // รายวัน
}

/// <summary>สถานะการเป็นพนักงาน</summary>
public enum EmploymentStatus
{
    Active = 1,    // ปกติ
    Resigned = 2,  // ลาออก
}

/// <summary>สถานะผู้ประกันตน (ประกันสังคม) ของพนักงาน</summary>
public enum SsoMemberStatus
{
    NotEnrolled = 0,  // ยังไม่แจ้งเข้า
    Enrolled = 1,     // แจ้งเข้าแล้ว
    Terminated = 2,   // แจ้งออกแล้ว
}

/// <summary>ประเภทเอกสาร/หลักฐานของพนักงาน (PDPA-sensitive)</summary>
public enum EmployeeDocType
{
    IdCardFront = 1,        // รูปหน้าบัตรประชาชน
    SsoEnrollProof = 2,     // หลักฐานแจ้งเข้า ปกส.
    SsoTerminateProof = 3,  // หลักฐานแจ้งออก ปกส.
    Slip = 4,               // สลิปเงินเดือน
    Other = 99,             // อื่นๆ
}

/// <summary>ประเภทการแจ้ง ปกส.</summary>
public enum SsoEnrollmentType
{
    Enroll = 1,     // แจ้งเข้า
    Terminate = 2,  // แจ้งออก
}

/// <summary>สถานะการแจ้ง ปกส. (ขับ checklist)</summary>
public enum SsoEnrollmentStatus
{
    Pending = 0,    // รอแจ้ง
    Submitted = 1,  // แจ้งแล้ว
}

/// <summary>สถานะงวดเงินเดือนรายเดือน</summary>
public enum PayrollRunStatus
{
    Draft = 0,      // ร่าง (กำลังกรอก)
    Recorded = 1,   // บันทึกแล้ว (กระทบยอด/พร้อมยื่น)
    Closed = 2,     // ปิดงวด
}

/// <summary>บทบาทบัญชีในใบสำคัญลงบัญชีเงินเดือน (สำหรับ generate รายการ + กระทบยอด GL)</summary>
public enum PayrollPostingRole
{
    // ── เดบิต (ค่าใช้จ่าย) — แยกตามฝ่าย ──
    SalaryExpense = 1,        // เงินเดือน
    DailyWageExpense = 2,     // ค่าจ้างรายวัน
    AllowanceExpense = 3,     // เบี้ยเลี้ยง/OT/เบี้ยขยัน/โบนัส/อื่น (รวม)
    EmployerSsoExpense = 4,   // เงินสมทบนายจ้าง ปกส.
    // ── เครดิต (หนี้สิน/หักจ่าย) ──
    SsoPayable = 10,          // เงินประกันสังคมรอนำส่ง (ลูกจ้าง+นายจ้าง) — ทั้งบริษัท
    WhtPayable = 11,          // ภาษีหัก ณ ที่จ่ายค้างจ่าย — ทั้งบริษัท
    EmployeeDeductionCredit = 12, // หักจากพนักงาน (ขาดงาน+เบิกล่วงหน้า+หักอื่น) — แยกฝ่าย
    NetPayCredit = 13,        // เงินเดือน/ค่าจ้างสุทธิค้างจ่าย/จ่าย — แยกฝ่าย
}
