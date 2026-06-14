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

        // ── Backfill ที่อยู่แยกช่องจาก Address flat (ครั้งเดียว สำหรับบริษัทที่ import ก่อนมี column) ──
        var needAddr = await db.ClientCompanies
            .Where(c => c.Address != null && c.Address != ""
                     && c.AddrHouseNo == null && c.AddrMoo == null && c.AddrRoad == null
                     && c.AddrSubDistrict == null && c.AddrDistrict == null && c.AddrProvince == null)
            .ToListAsync();
        if (needAddr.Count > 0)
        {
            foreach (var c in needAddr)
            {
                var a = Datacenter.Application.Features.CorporateTax.Services.ThaiAddressParser.Parse(c.Address);
                c.AddrHouseNo = a.HouseNo; c.AddrMoo = a.Moo; c.AddrSoi = a.Soi; c.AddrRoad = a.Road;
                c.AddrSubDistrict = a.SubDistrict; c.AddrDistrict = a.District; c.AddrProvince = a.Province;
                if (string.IsNullOrWhiteSpace(c.PostalCode) && !string.IsNullOrWhiteSpace(a.PostalCode))
                    c.PostalCode = a.PostalCode;
            }
            await db.SaveChangesAsync();
            logger.LogInformation("Backfilled structured address for {Count} companies.", needAddr.Count);
        }

        // ── Backfill ผู้ลงนาม free-text เดิม (CompanyAuditor) → ทะเบียน master + ค่าเริ่มต้นบริษัท ──
        var legacy = await db.CompanyAuditors
            .Where(x => x.AuditorId == null && x.AuditorName != null && x.AuditorName != "")
            .ToListAsync();
        if (legacy.Count > 0)
        {
            foreach (var row in legacy)
            {
                // ผู้สอบบัญชี → หา/สร้าง master (dedupe ตามชื่อ+เลขผู้เสียภาษี)
                var a = await db.Auditors.FirstOrDefaultAsync(x =>
                    x.Name == row.AuditorName && x.TaxId == row.AuditorTaxId);
                if (a is null)
                {
                    a = new Auditor
                    {
                        Name = row.AuditorName!, Type = Datacenter.Domain.Enums.AuditorType.Cpa,
                        LicenseNo = row.AuditorLicenseNo, TaxId = row.AuditorTaxId,
                        AuditFirmName = row.AuditFirmName, AuditFirmTaxId = row.AuditFirmTaxId,
                        CreatedBy = "system-migrate",
                    };
                    db.Auditors.Add(a);
                    await db.SaveChangesAsync();
                }
                row.AuditorId = a.Id;

                if (!string.IsNullOrWhiteSpace(row.BookkeeperName))
                {
                    var b = await db.Bookkeepers.FirstOrDefaultAsync(x =>
                        x.Name == row.BookkeeperName && x.TaxId == row.BookkeeperTaxId);
                    if (b is null)
                    {
                        b = new Bookkeeper { Name = row.BookkeeperName!, TaxId = row.BookkeeperTaxId, CreatedBy = "system-migrate" };
                        db.Bookkeepers.Add(b);
                        await db.SaveChangesAsync();
                    }
                    row.BookkeeperId = b.Id;
                }

                // ตั้งเป็นค่าเริ่มต้นของบริษัท (ถ้ายังไม่มี) เพื่อให้ใช้ทุกปีอัตโนมัติ
                var company = await db.ClientCompanies.FirstOrDefaultAsync(c => c.Id == row.ClientCompanyId);
                if (company is not null)
                {
                    company.DefaultAuditorId ??= row.AuditorId;
                    if (row.BookkeeperId is not null) company.DefaultBookkeeperId ??= row.BookkeeperId;
                }
            }
            await db.SaveChangesAsync();
            logger.LogInformation("Backfilled {Count} legacy CompanyAuditor rows -> Auditor/Bookkeeper master.", legacy.Count);
        }

        // ── Seed taxonomy บรรทัด CIT50 รายการที่ 8 (รายจ่ายขายและบริหาร) — idempotent ──
        var cit50Seed = new (string Code, string Label, int Sort, double Y, bool Catch, bool Total)[]
        {
            ("R8_EMP", "รายจ่ายเกี่ยวกับพนักงาน", 1, 56.4, false, false),
            ("R8_DIR", "ค่าตอบแทนกรรมการ", 2, 75.1, false, false),
            ("R8_FREIGHT", "ค่าระวาง ค่าขนส่ง", 5, 131.8, false, false),
            ("R8_RENT", "ค่าเช่า", 6, 150.8, false, false),
            ("R8_ENT", "ค่ารับรอง", 8, 188.2, false, false),
            ("R8_SBT", "ค่าภาษีธุรกิจเฉพาะ", 10, 226.2, false, false),
            ("R8_TAXOTHER", "ค่าภาษีอากรอื่นๆ", 11, 245.6, false, false),
            ("R8_FIN", "ต้นทุนทางการเงิน", 12, 264.2, false, false),
            ("R8_BOOK", "ค่าทำบัญชี", 13, 282.9, false, false),
            ("R8_AUDIT", "ค่าสอบบัญชี", 14, 302.4, false, false),
            ("R8_CONSULT", "ค่าธรรมเนียมการให้คำปรึกษา", 25, 530.3, false, false),
            ("R8_FEEOTHER", "ค่าธรรมเนียมอื่นๆ", 26, 550.0, false, false),
            ("R8_BADDEBT", "หนี้สูญ", 27, 568.7, false, false),
            ("R8_DEPREC", "ค่าสึกหรอและค่าเสื่อมราคา", 28, 587.2, false, false),
            ("R8_OTHER", "รายจ่ายอื่น (1.-29.)", 29, 606.7, true, false),
            ("R8_TOTAL", "รวม 1. ถึง 30.", 30, 625.0, false, true),
        };
        var existingCodes = await db.Cit50ScheduleLines.Select(x => x.Code).ToListAsync();
        foreach (var s in cit50Seed.Where(s => !existingCodes.Contains(s.Code)))
            db.Cit50ScheduleLines.Add(new Cit50ScheduleLine
            {
                Code = s.Code, ScheduleNo = 8, Label = s.Label, SortOrder = s.Sort,
                PdfPage = 4, PdfX = 462.7, PdfY = s.Y, PdfW = 101.1,
                IsCatchAll = s.Catch, IsTotal = s.Total, CreatedBy = "system",
            });
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync();

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
