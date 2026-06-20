using Datacenter.Domain.Enums;

namespace Datacenter.Application.Features.Tasks.DTOs;

public record WorkTaskItemDto(int Id, string Text, bool IsDone, int SortOrder);

/// <summary>งานทั่วไป (ad-hoc) ต่อบริษัท</summary>
public record WorkTaskDto(
    int Id,
    int ClientCompanyId,
    string ClientName,
    string Title,
    string? Description,
    string? Category,
    int Status,
    string StatusName,
    int Priority,
    string PriorityName,
    DateTime? DueDate,
    int? AssignedUserId,
    string? AssignedUserName,
    DateTime? CompletedAt,
    string? CompletedByUserName,
    bool IsOverdue,
    int RecurrenceType,
    string RecurrenceName,
    int RecurrenceInterval,
    IReadOnlyList<WorkTaskItemDto> Items,
    int DoneCount,
    int TotalCount,
    DateTime CreatedAt,
    string? CreatedBy);

/// <summary>รายการงานรวม (workboard ข้ามบริษัท) — รวม WorkTask + ComplianceTask</summary>
public record WorkItemDto(
    string Source,            // "Task" | "Compliance"
    int Id,
    int ClientCompanyId,
    string ClientName,
    string Title,
    int Status,
    string StatusName,
    int? Priority,
    string? PriorityName,
    DateTime? DueDate,
    int? AssignedUserId,
    string? AssignedUserName,
    bool IsOverdue,
    int? DaysToDue);

public record WorkTaskItemInput(string Text, bool IsDone);

/// <summary>สรุปผลส่งอีเมลเตือนงาน</summary>
public record TaskReminderResultDto(int Sent, int Skipped, int Failed, IReadOnlyList<string> Messages);

public static class WorkTaskNames
{
    public static string StatusName(WorkTaskStatus s) => s switch
    {
        WorkTaskStatus.Open => "เปิด/รอทำ",
        WorkTaskStatus.InProgress => "กำลังทำ",
        WorkTaskStatus.Done => "เสร็จสิ้น",
        WorkTaskStatus.Cancelled => "ยกเลิก",
        _ => s.ToString(),
    };

    public static string PriorityName(WorkTaskPriority p) => p switch
    {
        WorkTaskPriority.Low => "ต่ำ",
        WorkTaskPriority.Normal => "ปกติ",
        WorkTaskPriority.High => "สูง",
        _ => p.ToString(),
    };

    public static string RecurrenceName(WorkTaskRecurrence r) => r switch
    {
        WorkTaskRecurrence.None => "ไม่ซ้ำ",
        WorkTaskRecurrence.Daily => "รายวัน",
        WorkTaskRecurrence.Weekly => "รายสัปดาห์",
        WorkTaskRecurrence.Monthly => "รายเดือน",
        WorkTaskRecurrence.Yearly => "รายปี",
        _ => r.ToString(),
    };
}
