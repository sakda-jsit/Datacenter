# Deploy — JSP Datacenter

ชุดสคริปต์/ค่าตั้งสำหรับนำระบบขึ้นใช้งานจริง (v1 ให้ผู้ใช้ทดลองใช้)

| ไฟล์ | ใช้ทำอะไร |
|---|---|
| `publish.ps1` | build frontend + backend เป็นชุดเดียว (frontend ไปอยู่ `wwwroot` ของ API) + ใส่สคริปต์ในโฟลเดอร์ `deploy\` ของชุดให้ด้วย |
| `install-service.ps1` | **ติดตั้งครั้งเดียวจบ**: ตรวจความพร้อม → สร้างค่าตั้ง (สุ่ม Jwt:Key) → สำรอง DB → ลงทะเบียน Windows service → firewall → งานสำรองรายวัน → สตาร์ต+ทดสอบ |
| `create-sql-login.ps1` | สร้าง SQL login เฉพาะงาน (เช่น `dc_app`) + ให้ db_owner เฉพาะฐานนี้ แทนการใช้ `sa` |
| `backup-db.ps1` | สำรองฐานข้อมูล + ตรวจไฟล์ backup (`RESTORE VERIFYONLY`) + ลบไฟล์เก่า |
| `appsettings.Production.example.json` | ตัวอย่างค่าตั้งครบทุก section (ถ้าอยากเขียนเองแทนให้สคริปต์สร้าง) |

> คู่มือเชิงนโยบาย (checklist, PDPA, สิ่งที่เป็นหน้าที่ IT) อยู่ที่ `docs/24-deployment.md`

## รูปแบบที่เลือกสำหรับ v1

```
ผู้ใช้ในสำนักงาน ──HTTP (วง LAN)──> Windows service "DatacenterApi" (Kestrel :5000) ──> SQL Server / DatacenterDb
                                              │
                                   wwwroot = หน้าจอ (ไฟล์นิ่งจาก Vite)
```

frontend กับ API อยู่ origin เดียวกัน → ไม่ต้องตั้ง CORS, ไม่ต้องมี web server แยก, ไม่ต้องมีใบรับรอง
(เมื่อพร้อมทำ HTTPS ค่อยเอา IIS มาวางหน้าเป็น reverse proxy — ดูหัวข้อท้ายไฟล์)

## ติดตั้งครั้งแรก — 4 คำสั่ง

```powershell
# 0) คัดลอกชุด deploy ทั้งโฟลเดอร์ไปที่เครื่อง server เช่น C:\Datacenter\app
#    (ชุดนี้สร้างจากเครื่อง dev ด้วย: .\deploy\publish.ps1 -OutputPath D:\Datacenter\release\v1)

# 1) ตรวจความพร้อมก่อน (ไม่แก้อะไร รันได้โดยไม่ต้องเป็น administrator)
cd C:\Datacenter\app\deploy
.\install-service.ps1 -AppPath C:\Datacenter\app -CheckOnly

# 2) สร้าง SQL login ให้ระบบ (ทำครั้งเดียว รันด้วยบัญชีที่เป็น sysadmin ของ SQL Server)
.\create-sql-login.ps1 -Server localhost -Database DatacenterDb -Login dc_app -Password '<รหัสยาว ๆ ที่สุ่มมา>'

# 3) ติดตั้ง (เปิด PowerShell แบบ Run as administrator)
.\install-service.ps1 -AppPath C:\Datacenter\app -Port 5000 `
    -SqlServer localhost -Database DatacenterDb -SqlUser dc_app -SqlPassword '<รหัสเดียวกับข้อ 2>' `
    -BackupDir D:\Backup\Datacenter
```

สคริปต์จะสรุปตอนจบว่าเข้าใช้งานที่ `http://<ชื่อเครื่อง>:5000/` — ผู้ใช้ในวง LAN เปิด URL นี้ได้เลย

**สิ่งที่สคริปต์ทำให้:** สร้าง `appsettings.Production.json` พร้อม `Jwt:Key` สุ่ม 48 ไบต์ (ถ้ามีไฟล์อยู่แล้วจะไม่ทับ) ·
ปิด HTTPS redirect (เพราะใช้ HTTP ใน LAN) · สำรอง DB ก่อนสตาร์ตครั้งแรก (ระบบอัปเกรด schema เองตอนสตาร์ต) ·
ตั้ง service เป็น Automatic + รีสตาร์ตเองเมื่อล้ม · เปิด firewall เฉพาะโปรไฟล์ Domain/Private ·
ตั้ง Task Scheduler สำรองข้อมูลรายวัน 01:30 · ทดสอบว่า API ตอบจริงก่อนจบ

**คำสั่งเสริม:** `-SkipBackup` (ข้ามสำรอง), `-SkipBackupTask` (ไม่ตั้งงานสำรอง),
`-ServiceUser DOMAIN\user -ServicePassword '***'` (ให้ service รันด้วยบัญชีนี้ — จำเป็นถ้าต้องอ่าน Express จาก network share),
`-Uninstall` (ถอน service + firewall + งานสำรอง โดยไม่ลบข้อมูลและไฟล์ค่าตั้ง)

## ⚠️ ข้อมูล Express ต้องเป็น UNC ไม่ใช่ mapped drive

Windows service มองไม่เห็น drive ที่ผู้ใช้ map ไว้ บนเครื่อง dev ปัจจุบัน `J:` = `\\js-server\ExpressI`
ดังนั้นใน `appsettings.Production.json` ต้องตั้ง

```json
"Import": {
  "ExpressBasePath": "\\\\js-server\\ExpressI\\",
  "SnapshotBasePath": "D:\\ExpressSnapshots"
}
```

และให้ service รันด้วยบัญชีที่มีสิทธิ์อ่าน share นั้น (`-ServiceUser`) — ถ้ารันเป็น LocalSystem จะอ่าน share ไม่ได้
(`install-service.ps1 -CheckOnly` เตือนให้เมื่อเจอ mapped drive)

## ผู้ใช้ชุดแรก

1. เปิด `http://<ชื่อเครื่อง>:5000/` → login `admin` (รหัสตั้งต้น `admin1234` ถ้ายังไม่เคยเปลี่ยน)
2. ระบบ**บังคับเปลี่ยนรหัสทันที** — ตั้งรหัสใหม่ (≥8 ตัว มีตัวอักษร+ตัวเลข)
3. เมื่อพร้อมเพิ่มทีม: เมนู **ระบบ → ผู้ใช้งานระบบ** สร้างบัญชีรายคน + เลือกบริษัทที่แต่ละคนเข้าถึงได้
   (ห้ามใช้บัญชี `admin` ร่วมกัน — audit log ต้องระบุตัวบุคคลได้)

## อัปเดตรุ่นถัดไป

```powershell
# ที่เครื่อง dev
.\deploy\publish.ps1 -OutputPath D:\Datacenter\release\v1

# ที่เครื่อง server
Stop-Service DatacenterApi
.\deploy\backup-db.ps1 -BackupDir D:\Backup\Datacenter     # สำรองก่อนทุกครั้ง (schema อัปเกรดอัตโนมัติ)
# คัดลอกไฟล์ชุดใหม่ทับ C:\Datacenter\app (ไม่ต้องแตะ appsettings.Production.json)
Start-Service DatacenterApi
```

## log และการตรวจสอบ

```powershell
Get-Service DatacenterApi
Get-Content C:\Datacenter\app\logs\datacenter-$(Get-Date -Format yyyyMMdd).log -Tail 50
```

log ไฟล์รายวัน เก็บ 90 วัน (ปรับที่ section `Logging:File`)

## ทางเลือก: IIS + HTTPS (เมื่อต้องการเข้าจากภายนอก)

- ติดตั้ง [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
- Application Pool: .NET CLR = **No Managed Code**; Site ชี้ที่โฟลเดอร์ชุด deploy; ผูก HTTPS + ใบรับรอง
- ตั้ง env `ASPNETCORE_ENVIRONMENT=Production` ให้ app pool และคง `Hosting:UseHttpsRedirection = false`
  (TLS จบที่ IIS แล้ว ระบบอ่าน `X-Forwarded-Proto` ผ่าน ForwardedHeaders อยู่)
- ถ้าใช้ IIS ก็ถอน Windows service ออกได้ด้วย `.\install-service.ps1 -Uninstall`
