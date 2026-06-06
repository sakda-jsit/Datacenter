using System.Text;
using Datacenter.Application.Common.Interfaces;
using Datacenter.Infrastructure.Identity;
using Datacenter.Infrastructure.Persistence;
using Datacenter.Infrastructure.Services;
using Datacenter.Infrastructure.Services.Email;
using Datacenter.Infrastructure.Services.Notes;
using Datacenter.Infrastructure.Services.Payroll;
using Datacenter.Infrastructure.Services.Wht;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

namespace Datacenter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IImportStorageService, ImportStorageService>();
        services.AddScoped<IExpressDbfAdapter, ExpressDbfAdapter>();
        services.AddHttpContextAccessor();

        // อีเมล (SMTP) — เจ้าหน้าที่กรอก credential ใน config "EmailSettings" ตอน deploy
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // หนังสือรับรองหัก ณ ที่จ่าย (QuestPDF) — license ฟรี Community + ฟอนต์ไทยจากระบบ
        QuestPDF.Settings.License = LicenseType.Community;
        var certFont = configuration["Wht:CertificateFont"] ?? "Tahoma";
        services.AddSingleton<IWhtCertificatePdfService>(_ => new WhtCertificatePdfService(certFont));
        services.AddSingleton<ISignatureImageProcessor, SignatureImageProcessor>();
        services.AddSingleton<IPayrollExcelService, PayrollExcelService>();
        services.AddSingleton<ISsoFilingExcelService, Services.Payroll.SsoFilingExcelService>();
        services.AddSingleton<ISsoFilingPdfService>(_ => new Services.Payroll.SsoFilingPdfService(certFont));
        services.AddSingleton<IPnd1kExportService>(_ => new Services.Payroll.Pnd1kExportService(certFont));
        services.AddSingleton<IKt20ExportService>(_ => new Services.Payroll.Kt20ExportService(certFont));

        // หมายเหตุประกอบงบ (NOTE2) export Excel รูปแบบงบ — ClosedXML
        services.AddScoped<INote2ExcelExporter, Note2ExcelExporter>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            });

        return services;
    }
}
