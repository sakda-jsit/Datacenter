using System.Text;
using Datacenter.Application.Common.Exceptions;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Tasks.DTOs;
using Datacenter.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Datacenter.Application.Features.Tasks.Commands;

/// <summary>
/// ส่งอีเมลเตือนผู้รับผิดชอบ — งานทั่วไป (WorkTask) ที่ยังไม่เสร็จและ overdue หรือใกล้ครบกำหนด ≤ DaysAhead วัน.
/// digest 1 ฉบับ/คน. ใช้ทั้ง manual (admin) และ background service (ไม่มี user → ข้ามเช็คสิทธิ).
/// ทำงานทุกบริษัท (งานเตือนระดับสำนักงาน) — ไม่ผูก per-company scope.
/// </summary>
public record SendTaskRemindersCommand(int DaysAhead = 3) : IRequest<TaskReminderResultDto>;

public class SendTaskRemindersCommandHandler(
    IApplicationDbContext db, IEmailSender email, ICurrentUserService currentUser)
    : IRequestHandler<SendTaskRemindersCommand, TaskReminderResultDto>
{
    public async Task<TaskReminderResultDto> Handle(SendTaskRemindersCommand request, CancellationToken ct)
    {
        // manual trigger ผ่าน HTTP ต้องเป็น Admin; background (ไม่มี user) ข้ามเช็ค
        if (currentUser.IsAuthenticated && currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("เฉพาะผู้ดูแลระบบ (Admin) เท่านั้นที่ส่งอีเมลเตือนงานได้");

        var today = DateTime.UtcNow.Date;
        var windowEnd = today.AddDays(Math.Max(request.DaysAhead, 0));

        var tasks = await db.WorkTasks.AsNoTracking()
            .Include(t => t.ClientCompany).Include(t => t.AssignedUser)
            .Where(t => t.AssignedUserId != null
                     && (t.Status == WorkTaskStatus.Open || t.Status == WorkTaskStatus.InProgress)
                     && t.DueDate != null && t.DueDate <= windowEnd)
            .ToListAsync(ct);

        int sent = 0, skipped = 0, failed = 0;
        var messages = new List<string>();

        foreach (var grp in tasks.GroupBy(t => t.AssignedUserId!.Value))
        {
            var user = grp.First().AssignedUser;
            var name = user?.DisplayName ?? $"user#{grp.Key}";
            var to = user?.Email;
            if (string.IsNullOrWhiteSpace(to))
            {
                skipped++;
                messages.Add($"ข้าม {name}: ไม่มีอีเมล ({grp.Count()} งาน)");
                continue;
            }

            try
            {
                await email.SendAsync(BuildMessage(to!, name, grp.OrderBy(t => t.DueDate).ToList(), today), ct);
                sent++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                messages.Add($"ล้มเหลว {name}: {ex.Message}");
            }
        }

        if (sent == 0 && skipped == 0 && failed == 0)
            messages.Add("ไม่มีงานที่ต้องเตือนในช่วงนี้");

        return new TaskReminderResultDto(sent, skipped, failed, messages);
    }

    private static EmailMessage BuildMessage(string to, string name, List<Domain.Entities.WorkTask> tasks, DateTime today)
    {
        var sb = new StringBuilder();
        sb.Append($"<p>เรียน {System.Net.WebUtility.HtmlEncode(name)},</p>");
        sb.Append($"<p>คุณมีงานที่ต้องติดตาม {tasks.Count} รายการ:</p>");
        sb.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;font-family:sans-serif;font-size:13px'>");
        sb.Append("<tr style='background:#f1f5f9'><th>งาน</th><th>บริษัท</th><th>กำหนดส่ง</th><th>สถานะ</th></tr>");
        foreach (var t in tasks)
        {
            bool overdue = t.DueDate!.Value.Date < today;
            var company = string.IsNullOrWhiteSpace(t.ClientCompany?.LegalName) ? t.ClientCompany?.Name : t.ClientCompany!.LegalName;
            var due = t.DueDate.Value.ToString("yyyy-MM-dd");
            var tag = overdue
                ? "<span style='color:#dc2626;font-weight:bold'>เกินกำหนด</span>"
                : "<span style='color:#d97706'>ใกล้ครบกำหนด</span>";
            sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(t.Title)}</td>"
                    + $"<td>{System.Net.WebUtility.HtmlEncode(company)}</td>"
                    + $"<td>{due}</td><td>{tag}</td></tr>");
        }
        sb.Append("</table>");
        sb.Append("<p style='color:#64748b;font-size:12px'>อีเมลนี้ส่งจากระบบ Datacenter (สำนักงานบัญชี) โดยอัตโนมัติ</p>");

        return new EmailMessage(to, $"แจ้งเตือนงานค้าง/ใกล้ครบกำหนด ({tasks.Count} รายการ)", sb.ToString(), []);
    }
}
