namespace Datacenter.Application.Features.Dashboard.DTOs;

/// <summary>หนึ่งช่องในตารางงานตามบริษัท (สถานะของงานประเภทหนึ่งในเดือนนั้น)</summary>
public record WorkTrackerCellDto(
    int TaskType, string TaskTypeName, int Status, string StatusName, bool IsOverdue, int TaskId);

/// <summary>หนึ่งแถวบริษัทในตารางภาพรวม</summary>
public record WorkTrackerCompanyRowDto(
    int ClientCompanyId, string ClientName,
    int Total, int Completed, int Open, int Overdue,
    IReadOnlyList<WorkTrackerCellDto> Cells);

/// <summary>งานที่ต้องจัดการด่วน (เกินกำหนด/ใกล้ครบกำหนด) ข้ามบริษัท</summary>
public record WorkTrackerAttentionDto(
    int TaskId, int ClientCompanyId, string ClientName,
    int TaskType, string TaskTypeName, DateTime DueDate,
    int Status, string StatusName, bool IsOverdue, int DaysToDue);

/// <summary>ภาพรวมงานประจำทุกบริษัทของงวด (ปี/เดือน) — โหมด A ของ Dashboard</summary>
public record WorkTrackerOverviewDto(
    int Year, int Month,
    int TotalTasks, int Completed, int InProgress, int Pending, int Overdue, int DueSoon,
    int CompaniesWithOpenWork, int CompaniesWithTasks, int TotalActiveCompanies, int CompaniesNoTasks,
    IReadOnlyList<WorkTrackerAttentionDto> NeedsAttention,
    IReadOnlyList<WorkTrackerCompanyRowDto> Companies);
