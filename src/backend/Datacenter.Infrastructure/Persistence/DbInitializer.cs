using Datacenter.Application.Common.Interfaces;
using Datacenter.Application.Features.Import;
using Datacenter.Application.Features.Import.DTOs;
using Datacenter.Domain.Entities;
using Datacenter.Domain.Enums;
using Datacenter.Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Datacenter.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope         = serviceProvider.CreateScope();
        var db                  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher      = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var expressAdapter      = scope.ServiceProvider.GetRequiredService<IExpressDbfAdapter>();
        var configuration       = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger              = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await db.Database.MigrateAsync();

        // ── Seed ClientCompanies จาก Express ─────────────────────────────────
        if (!await db.ClientCompanies.AnyAsync())
        {
            var basePath = configuration["Import:ExpressBasePath"];

            if (!string.IsNullOrWhiteSpace(basePath) && Directory.Exists(basePath))
            {
                // อ่านทะเบียนข้อมูล Express แล้วคัดเฉพาะบริษัทปัจจุบัน
                // (ตัด X-/Z- = ปีเก่า/สำเนา และ CANDEL=N ตามกฎใน ExpressDatasetFilter)
                IReadOnlyList<ExpressDatasetDto> registry;
                try
                {
                    registry = await expressAdapter.ReadCompanyRegistryAsync(basePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("อ่านทะเบียนข้อมูล sccomp.dbf ไม่ได้: {Message}", ex.Message);
                    registry = [];
                }

                var current = registry.Where(ExpressDatasetFilter.IsCurrentCompany).ToList();
                logger.LogInformation(
                    "ทะเบียน Express: {Total} รายการ, เป็นบริษัทปัจจุบัน {Current} รายการ",
                    registry.Count, current.Count);

                var added = 0;
                foreach (var dataset in current)
                {
                    try
                    {
                        var folder = Path.Combine(basePath, dataset.Path);
                        if (!Directory.Exists(folder))
                        {
                            logger.LogWarning("ข้าม {Code}: ไม่พบโฟลเดอร์ {Folder}", dataset.Path, folder);
                            continue;
                        }
                        if (!await expressAdapter.FolderIsValidAsync(folder))
                        {
                            logger.LogWarning("ข้าม {Code}: ไม่พบ ISINFO ในโฟลเดอร์", dataset.Path);
                            continue;
                        }

                        var code = dataset.Path.ToUpper();
                        if (await db.ClientCompanies.AnyAsync(c => c.Code == code)) continue;

                        var info = await expressAdapter.ReadCompanyInfoAsync(folder);
                        var expressName = !string.IsNullOrWhiteSpace(info.ThaiName) ? info.ThaiName
                                        : !string.IsNullOrWhiteSpace(info.EngName) ? info.EngName
                                        : dataset.CompName;

                        db.ClientCompanies.Add(new ClientCompany
                        {
                            Code                 = code,
                            Name                 = expressName,
                            LegalName            = expressName,   // default = ชื่อ Express; แก้ไขได้ภายหลัง
                            EnglishName          = string.IsNullOrWhiteSpace(info.EngName) ? null : info.EngName,
                            Address              = info.Address,
                            TaxId                = info.TaxId,
                            BranchCode           = "00000",
                            FiscalYearStartMonth = 1,
                            IsActive             = true,
                            CreatedAt            = DateTime.UtcNow,
                            CreatedBy            = "system",
                        });
                        added++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("ข้าม {Code}: {Message}", dataset.Path, ex.Message);
                    }
                }

                if (added > 0)
                {
                    await db.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} client companies from Express ({Path}).", added, basePath);
                }
                else
                {
                    logger.LogWarning("ไม่พบบริษัทปัจจุบันที่ seed ได้จากทะเบียน Express ({Path})", basePath);
                }
            }
            else
            {
                logger.LogWarning("Express base path ไม่ถูกต้องหรือไม่มีอยู่: {Path}", basePath);
            }
        }

        // ── Seed admin user ───────────────────────────────────────────────────
        if (!await db.Users.AnyAsync(u => u.Username == "admin"))
        {
            db.Users.Add(new User
            {
                Username     = "admin",
                PasswordHash = passwordHasher.Hash("admin1234"),
                DisplayName  = "Administrator",
                Role         = UserRole.Admin,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
                CreatedBy    = "system",
            });

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded admin user.");
        }

        // ── Seed StatementLines (บรรทัดงบการเงินมาตรฐาน, master reference) ──────
        if (!await db.StatementLines.AnyAsync())
        {
            db.StatementLines.AddRange(StatementLineSeed.GetLines());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded statement lines (FS master reference).");
        }

        // ── Seed NoteTemplateSections (ข้อความหมายเหตุประกอบงบ TFRS-NPAEs, master กลาง) ──
        if (!await db.NoteTemplateSections.AnyAsync())
        {
            db.NoteTemplateSections.AddRange(NoteTemplateSeed.GetSections());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded NOTE2 template sections (financial statement notes).");
        }

        // ── Seed AssetTypeMasters (ประเภทสินทรัพย์ + อัตราค่าเสื่อมมาตรฐาน) ──────
        if (!await db.AssetTypeMasters.AnyAsync())
        {
            db.AssetTypeMasters.AddRange(AssetTypeSeed.GetTypes());
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded asset type masters (FA depreciation rates).");
        }

        // ── Seed อัตราเงินสมทบ ปกส./กองทุนทดแทน (ค่ากลาง effective-dated) ──────────
        if (!await db.PayrollRateConfigs.AnyAsync())
        {
            db.PayrollRateConfigs.Add(new PayrollRateConfig
            {
                ClientCompanyId = null,                 // ค่ากลางทุกบริษัท
                EffectiveFrom = new DateTime(2024, 1, 1),
                SsoEmployeePct = 5m, SsoEmployerPct = 5m,
                SsoWageFloor = 1650m, SsoWageCap = 15000m,
                WcfRatePct = 0.2m, WcfWageCapPerYear = 240000m,
                Note = "อัตรามาตรฐาน (ปรับ/เพิ่มแถวมีผลตามวันที่ได้)",
                CreatedBy = "system",
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded default payroll rate config (SSO/WCF).");
        }
    }
}
