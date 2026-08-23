using Datacenter.Api.Configuration;
using Datacenter.Api.Logging;
using Datacenter.Api.Middleware;
using Datacenter.Application;
using Datacenter.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ----- Configuration -----
// appsettings.Local.json = ค่าเฉพาะเครื่อง (connection string / Jwt:Key) — ไม่ commit ลง git.
// บน production ใช้ environment variable แทนได้ทั้งหมด เช่น ConnectionStrings__DefaultConnection, Jwt__Key
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
// ใส่ environment variable ทับอีกครั้งหลังไฟล์ Local เพื่อคง "ลำดับความสำคัญ" เดิมของ ASP.NET Core
// (env var ต้องชนะไฟล์ config เสมอ — production ตั้งค่าผ่าน env var ของ service/app pool)
builder.Configuration.AddEnvironmentVariables();

// ตรวจค่าตั้งที่ขาดไม่ได้ก่อนสตาร์ต — ล้มทันทีพร้อมบอกวิธีแก้ ดีกว่าเปิดระบบด้วยกุญแจตัวอย่าง
StartupConfigValidator.Validate(builder.Configuration, builder.Environment.IsDevelopment());

// ----- Services -----
builder.Logging.AddFileLogger(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Datacenter API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// CORS: origin ของ frontend อ่านจาก config "Cors:AllowedOrigins" (ตั้งตอน deploy).
// ถ้า deploy แบบ serve frontend จาก API เดียวกัน (same-origin) ไม่ต้องตั้ง — จะไม่เปิด CORS เลย
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? (builder.Environment.IsDevelopment() ? ["http://localhost:5173", "http://localhost:5174"] : []);
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()));
}

// อยู่หลัง reverse proxy (IIS/nginx) — ให้ ASP.NET รู้ scheme/ip จริงของผู้ใช้
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ----- Pipeline -----
var app = builder.Build();

// Seed database (migrate + ข้อมูลตั้งต้น)
await Datacenter.Infrastructure.Persistence.DbInitializer.SeedAsync(app.Services);

app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ใน Development ไม่บังคับ HTTPS redirect เพราะ Vite proxy เรียกผ่าน HTTP (localhost:5229)
// การ 307 redirect ข้าม origin (5229 -> 7065) ทำให้ browser ตัด Authorization header ทิ้ง => 401 => เด้ง login ซ้ำ
// บน production ปิดได้ด้วย Hosting:UseHttpsRedirection=false เมื่อ TLS จบที่ reverse proxy แล้ว
if (!app.Environment.IsDevelopment()
    && app.Configuration.GetValue("Hosting:UseHttpsRedirection", true))
    app.UseHttpsRedirection();

if (allowedOrigins.Length > 0) app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// เสิร์ฟ frontend (ไฟล์ build ของ Vite ใน wwwroot) จาก process เดียวกัน — deploy ง่าย, ไม่ต้องตั้ง CORS
// เส้นทางที่ไม่ใช่ /api ให้ตกที่ index.html เพื่อให้ react-router จัดการ (SPA fallback)
if (Directory.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot")))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    // fallback เฉพาะเส้นทางที่ไม่ได้ขึ้นต้นด้วย api/ เพื่อให้ endpoint ที่ไม่มีจริงใต้ /api ยังคืน 404 ตามปกติ
    app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");
}

app.Run();
