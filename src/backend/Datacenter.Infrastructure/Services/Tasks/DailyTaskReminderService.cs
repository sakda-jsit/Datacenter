using Datacenter.Application.Features.Tasks.Commands;
using Datacenter.Infrastructure.Services.Email;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Datacenter.Infrastructure.Services.Tasks;

/// <summary>
/// ส่งอีเมลเตือนงานอัตโนมัติวันละครั้ง (opt-in). เปิดเฉพาะเมื่อ <c>TaskReminders:Enabled=true</c>
/// และตั้งค่า SMTP แล้ว — ไม่งั้นไม่ทำอะไร (กันส่งเมลโดยไม่ตั้งใจ). ระบบไม่มี scheduler อื่น จึงใช้
/// BackgroundService + PeriodicTimer (เช็กทุกชั่วโมง, รันจริงวันละครั้งหลังเวลาที่กำหนด).
/// </summary>
public class DailyTaskReminderService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    IOptions<EmailSettings> emailOptions,
    ILogger<DailyTaskReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue<bool>("TaskReminders:Enabled"))
        {
            logger.LogInformation("DailyTaskReminderService ปิดอยู่ (TaskReminders:Enabled=false) — ไม่ส่งอีเมลเตือนอัตโนมัติ");
            return;
        }

        int daysAhead = config.GetValue<int?>("TaskReminders:DaysAhead") ?? 3;
        int sendHourUtc = config.GetValue<int?>("TaskReminders:SendHourUtc") ?? 1;
        DateOnly? lastRun = null;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                var nowUtc = DateTime.UtcNow;
                var todayUtc = DateOnly.FromDateTime(nowUtc);
                if (lastRun != todayUtc && nowUtc.Hour >= sendHourUtc && emailOptions.Value.IsConfigured)
                {
                    lastRun = todayUtc;
                    using var scope = scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var result = await mediator.Send(new SendTaskRemindersCommand(daysAhead), stoppingToken);
                    logger.LogInformation(
                        "Daily task reminders: sent={Sent} skipped={Skipped} failed={Failed}",
                        result.Sent, result.Skipped, result.Failed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DailyTaskReminderService รอบนี้ล้มเหลว");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
