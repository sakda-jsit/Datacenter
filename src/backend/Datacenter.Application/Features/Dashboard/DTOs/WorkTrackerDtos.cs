namespace Datacenter.Application.Features.Dashboard.DTOs;

/// <summary>หนึ่งช่องในตารางงานตามบริษัท (สถานะของงานประเภทหนึ่งในเดือนนั้น)</summary>
public record WorkTrackerCellDto(
    int TaskType, string TaskTypeName, int Status, string StatusName, bool IsOverdue, int TaskId);

/// <summary>หนึ่งแถวบริษัทในตารางภาพรวม</summary>
public record WorkTrackerCompanyRowDto(
    int ClientCompanyId, string ClientName,
    int Total, int Completed, int Open, int Overdue,
    IReadOnlyList<WorkTrackerCellDto> Cells);

/// <summary>งานที่ต้องจัดการด่วน (เกินกำหนด/ใกล้ครบกำหนด) ข้ามบริษัท <b>และข้ามงวด</b></summary>
public record WorkTrackerAttentionDto(
    int TaskId, int ClientCompanyId, string ClientName,
    int TaskType, string TaskTypeName, DateTime DueDate,
    int Status, string StatusName, bool IsOverdue, int DaysToDue,
    /// <summary>งวดของงาน เช่น "ส.ค. 2026", "ปีบัญชี 2026" — งานรอบยาวครบกำหนดคนละเดือนกับงวด</summary>
    string PeriodLabel);

/// <summary>คอลัมน์ประเภทงานที่ต้องแสดงในตารางของงวดนั้น (ขึ้นกับว่ามีงานอะไรจริง)</summary>
public record WorkTrackerColumnDto(int TaskType, string ShortName, string TaskTypeName);

/// <summary>ภาพรวมงานประจำทุกบริษัทของงวด (ปี/เดือน) — โหมด A ของ Dashboard</summary>
public record WorkTrackerOverviewDto(
    int Year, int Month,
    /// <summary>true = แสดงเฉพาะบริษัทที่ผู้ใช้ดูแล (ไม่ใช่ Admin) — ใช้บอกผู้ใช้ว่าตัวเลขครอบคลุมแค่ไหน</summary>
    bool ScopedToOwnedCompanies,
    int TotalTasks, int Completed, int InProgress, int Pending, int Overdue, int DueSoon,
    int CompaniesWithOpenWork, int CompaniesWithTasks, int TotalActiveCompanies, int CompaniesNoTasks,
    IReadOnlyList<WorkTrackerAttentionDto> NeedsAttention,
    IReadOnlyList<WorkTrackerCompanyRowDto> Companies,
    /// <summary>คอลัมน์ของตาราง — สร้างจากงานที่มีจริงในงวดนี้ ไม่ใช่รายการตายตัว</summary>
    IReadOnlyList<WorkTrackerColumnDto> Columns);
