using Datacenter.Application.Features.ComplianceCalendar.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Datacenter.Infrastructure.Services.Tasks;

/// <summary>
/// สร้างงานในปฏิทินงานให้ทุกบริษัทอัตโนมัติ — ตั้งค่างานประจำไว้แล้วไม่ต้องมากดสร้างเองรายบริษัท.
/// ทำงาน idempotent (งานที่มีอยู่แล้วถูกข้าม) จึงรันซ้ำได้ปลอดภัย: เช็กตอนเปิดระบบ แล้ววนทุกชั่วโมง
/// สร้างของ <b>เดือนปัจจุบัน</b> ให้บริษัทที่ยังขาด — ข้ามเดือนใหม่เมื่อไรก็ได้งานใหม่เอง
/// โดยไม่ต้องมี scheduler ภายนอก.
/// <para>ปิดได้ด้วย <c>TaskGeneration:Enabled=false</c> (ค่าเริ่มต้น = เปิด)</para>
/// </summary>
public class ComplianceTaskGeneratorService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<ComplianceTaskGeneratorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (config.GetValue<bool?>("TaskGeneration:Enabled") == false)
        {
            logger.LogInformation("ComplianceTaskGeneratorService ปิดอยู่ (TaskGeneration:Enabled=false)");
            return;
        }

        // หน่วงสั้น ๆ ให้ migration/seed ตอนเปิดระบบเสร็จก่อน
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                var now = DateTime.Now;   // งวดงานอิงปฏิทินไทยตามเวลาเครื่อง ไม่ใช่ UTC
                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(
                    new EnsureAllCompaniesTasksCommand(now.Year, now.Month, "system"), stoppingToken);

                if (result.Created > 0)
                    logger.LogInformation(
                        "สร้างงานปฏิทินอัตโนมัติ {Year}/{Month:D2}: {Created} งาน ใน {Touched}/{Checked} บริษัท",
                        result.Year, result.Month, result.Created, result.CompaniesTouched, result.CompaniesChecked);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ComplianceTaskGeneratorService รอบนี้ล้มเหลว");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
