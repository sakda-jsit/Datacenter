# Deploy — JSP Datacenter

ชุดสคริปต์/ค่าตั้งสำหรับนำระบบขึ้นใช้งานจริง (v1 ให้ผู้ใช้ทดลองใช้)

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `publish.ps1` | build frontend + backend เป็นชุดเดียว (frontend ไปอยู่ `wwwroot` ของ API) |
| `appsettings.Production.example.json` | ตัวอย่างค่าตั้ง production — คัดลอกเป็น `appsettings.Production.json` ที่เครื่อง server |
| `backup-db.ps1` | สำรองฐานข้อมูล + ตรวจไฟล์ backup + ลบไฟล์เก่า |

> คู่มือฉบับเต็ม (checklist + PDPA + การตั้งผู้ใช้) อยู่ที่ `docs/24-deployment.md`

## สถาปัตยกรรมการ deploy ที่แนะนำ (ง่ายที่สุด)

```
ผู้ใช้ ──HTTPS──> IIS (reverse proxy / TLS) ──HTTP──> Kestrel :5000  ──> SQL Server
                                                       │
                                            wwwroot = frontend (ไฟล์นิ่งจาก Vite)
```

frontend กับ API อยู่ origin เดียวกัน → **ไม่ต้องตั้ง CORS** และไม่ต้องมี web server แยก

## ขั้นตอนครั้งแรก

1. **เตรียมฐานข้อมูล**
   - สร้างฐานข้อมูลเปล่า `DatacenterDb` (ระบบ migrate โครงสร้างเองตอนสตาร์ตครั้งแรก)
   - สร้าง SQL login เฉพาะงานนี้ เช่น `dc_app` และให้เป็น `db_owner` ของ `DatacenterDb` เท่านั้น
     (ต้องมีสิทธิ์สร้าง/แก้ตาราง เพราะระบบรัน EF migrations ตอนสตาร์ต) — **ห้ามใช้ `sa`**

2. **build ชุด deploy** (ทำที่เครื่อง dev)
   ```powershell
   .\deploy\publish.ps1 -OutputPath D:\Datacenter\app
   ```

3. **ตั้งค่า** ที่เครื่อง server
   ```powershell
   Copy-Item .\deploy\appsettings.Production.example.json D:\Datacenter\app\appsettings.Production.json
   # แก้ ConnectionStrings:DefaultConnection และ Jwt:Key
   # สร้าง Jwt:Key แบบสุ่ม:
   [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
   ```
   ระบบ **จะไม่สตาร์ต** ถ้า `Jwt:Key` ว่าง สั้นกว่า 32 ตัวอักษร หรือยังเป็นค่าตัวอย่างในซอร์สโค้ด

4. **เปิดใช้งาน** — เลือกอย่างใดอย่างหนึ่ง

   **ก) IIS** (แนะนำบน Windows Server)
   - ติดตั้ง [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) (ให้ IIS รู้จัก .NET 8)
   - สร้าง Application Pool: .NET CLR version = **No Managed Code**, Identity = บัญชีที่อ่าน path Express ได้
   - สร้าง Site ชี้ที่ `D:\Datacenter\app` ผูก HTTPS + ใบรับรอง
   - ตั้ง environment variable ของ app pool: `ASPNETCORE_ENVIRONMENT=Production`
   - ใน `appsettings.Production.json` ตั้ง `Hosting:UseHttpsRedirection = false` (TLS จบที่ IIS แล้ว)

   **ข) Windows service** (Kestrel ตรง ๆ + reverse proxy อะไรก็ได้)
   ```powershell
   New-Service -Name DatacenterApi -BinaryPathName '"D:\Datacenter\app\Datacenter.Api.exe"' -StartupType Automatic
   [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT','Production','Machine')
   Start-Service DatacenterApi
   ```

5. **เข้าใช้งานครั้งแรก**
   - login `admin` / `admin1234` → ระบบ**บังคับเปลี่ยนรหัสทันที** (รหัสตั้งต้นใช้งานต่อไม่ได้)
   - ไปเมนู **ระบบ → ผู้ใช้งานระบบ** สร้างบัญชีรายคนให้พนักงาน + เลือกบริษัทที่แต่ละคนเข้าถึงได้
   - พนักงานที่ถูกสร้างใหม่จะถูกบังคับเปลี่ยนรหัสของตัวเองตอน login ครั้งแรกเช่นกัน

6. **ตั้งงานสำรองข้อมูลรายวัน** (Task Scheduler)
   ```powershell
   $action  = New-ScheduledTaskAction -Execute 'powershell.exe' `
       -Argument '-NonInteractive -ExecutionPolicy Bypass -File "D:\Datacenter\app\deploy\backup-db.ps1" -BackupDir D:\Backup\Datacenter'
   $trigger = New-ScheduledTaskTrigger -Daily -At 1:30am
   Register-ScheduledTask -TaskName 'Datacenter DB backup' -Action $action -Trigger $trigger -RunLevel Highest
   ```

## อัปเดตรุ่นถัดไป

```powershell
Stop-Service DatacenterApi        # หรือ Stop-WebAppPool ถ้าใช้ IIS
.\deploy\backup-db.ps1            # สำรองก่อนทุกครั้ง (ระบบ migrate ฐานข้อมูลอัตโนมัติตอนสตาร์ต)
.\deploy\publish.ps1 -OutputPath D:\Datacenter\app
Start-Service DatacenterApi
```

`publish.ps1` ไม่ทับ `appsettings.Production.json` เดิม และไม่คัดลอก `appsettings.Local.json` ของเครื่อง dev ขึ้นไป

## log

ระบบเขียน log ลงไฟล์รายวันที่ `D:\Datacenter\app\logs\datacenter-yyyyMMdd.log` (เก็บ 90 วัน)
ปรับได้ที่ section `Logging:File` — ดู `appsettings.Production.example.json`
