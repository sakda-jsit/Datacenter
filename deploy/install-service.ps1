<#
.SYNOPSIS
    ติดตั้ง JSP Datacenter เป็น Windows service (HTTP ในวง LAN) ครั้งเดียวจบ

.DESCRIPTION
    ทำให้ครบในคำสั่งเดียว: ตรวจความพร้อมของเครื่อง -> สร้าง appsettings.Production.json
    (สุ่ม Jwt:Key ให้เอง) -> สำรองฐานข้อมูลก่อนอัปเกรด schema -> ลงทะเบียน service +
    ตั้งให้รีสตาร์ตเองเมื่อล้ม -> เปิด firewall -> ตั้งงานสำรองข้อมูลรายวัน -> สตาร์ต + ทดสอบเรียก API

    ออกแบบให้รันซ้ำได้ (idempotent): ของที่มีอยู่แล้วจะไม่ถูกทับ โดยเฉพาะ appsettings.Production.json
    ที่เก็บ Jwt:Key และรหัสฐานข้อมูล

    เขียนให้รองรับ Windows PowerShell 5.1 (ไม่ใช้ ?. / ?? / ternary)

.EXAMPLE
    # ตรวจความพร้อมก่อน ไม่แก้อะไรเลย (รันได้โดยไม่ต้องเป็น administrator)
    .\install-service.ps1 -CheckOnly

.EXAMPLE
    # ติดตั้งจริง (ต้องเปิด PowerShell แบบ Run as administrator)
    .\install-service.ps1 -AppPath C:\Datacenter\app -SqlServer localhost -SqlUser dc_app -SqlPassword '***'

.EXAMPLE
    # ถอนการติดตั้ง (ไม่ลบฐานข้อมูลและไม่ลบไฟล์ค่าตั้ง)
    .\install-service.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [string]$AppPath = 'C:\Datacenter\app',
    [string]$ServiceName = 'DatacenterApi',
    [int]$Port = 5000,
    [string]$SqlServer = 'localhost',
    [string]$Database = 'DatacenterDb',
    # เว้นว่าง = ใช้ Windows authentication ของบัญชีที่ service รันด้วย
    [string]$SqlUser,
    [string]$SqlPassword,
    # บัญชีที่ service ใช้รัน — ต้องระบุถ้าข้อมูล Express อยู่บน network share
    [string]$ServiceUser,
    [string]$ServicePassword,
    [string]$BackupDir = 'D:\Backup\Datacenter',
    [switch]$SkipBackup,
    [switch]$SkipBackupTask,
    [switch]$CheckOnly,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'
# ให้ข้อความไทยแสดงถูกในคอนโซลที่ไม่ได้ตั้ง UTF-8
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$problems = @()
$notes = @()

function Section($text) { Write-Host "`n== $text ==" -ForegroundColor Cyan }
function Ok($text)      { Write-Host "  [ok] $text" -ForegroundColor Green }
function Warn($text)    { Write-Host "  [เตือน] $text" -ForegroundColor Yellow; $script:notes += $text }
function Bad($text)     { Write-Host "  [ขาด] $text" -ForegroundColor Red; $script:problems += $text }

function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($id)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-Sqlcmd {
    $cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $candidate = Get-ChildItem 'C:\Program Files\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe',
                               'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\sqlcmd.exe' `
                               -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($candidate) { return $candidate.FullName }
    return $null
}

function Invoke-Sql($sqlcmdPath, $query) {
    $auth = @('-E')
    if ($SqlUser) { $auth = @('-U', $SqlUser, '-P', $SqlPassword) }
    & $sqlcmdPath -S $SqlServer @auth -C -b -h -1 -W -Q $query
    return $LASTEXITCODE
}

$exePath = Join-Path $AppPath 'Datacenter.Api.exe'
$prodConfig = Join-Path $AppPath 'appsettings.Production.json'
$firewallRule = "JSP Datacenter (TCP $Port)"
$backupTaskName = 'Datacenter DB backup'

# ── ถอนการติดตั้ง ──────────────────────────────────────────────────────────────
if ($Uninstall) {
    if (-not (Test-Admin)) { throw "ต้องรันด้วยสิทธิ์ administrator (คลิกขวา PowerShell -> Run as administrator)" }
    Section "ถอนการติดตั้ง"
    $svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($svc) {
        if ($svc.Status -ne 'Stopped') { Stop-Service $ServiceName -Force; Ok "หยุด service" }
        & sc.exe delete $ServiceName | Out-Null
        Ok "ลบ service $ServiceName"
    } else { Warn "ไม่พบ service $ServiceName" }

    if (Get-NetFirewallRule -DisplayName $firewallRule -ErrorAction SilentlyContinue) {
        Remove-NetFirewallRule -DisplayName $firewallRule
        Ok "ลบกฎ firewall"
    }
    if (Get-ScheduledTask -TaskName $backupTaskName -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $backupTaskName -Confirm:$false
        Ok "ลบงานสำรองข้อมูลรายวัน"
    }
    Write-Host "`nถอนการติดตั้งแล้ว (ไม่ได้ลบฐานข้อมูล ไฟล์แอป และ appsettings.Production.json)" -ForegroundColor Green
    return
}

# ── 1) ตรวจความพร้อม ──────────────────────────────────────────────────────────
Section "ตรวจความพร้อมของเครื่อง"

if (Test-Admin) { Ok "รันด้วยสิทธิ์ administrator" }
elseif ($CheckOnly) { Warn "ไม่ได้รันด้วยสิทธิ์ administrator (ตรวจได้ แต่ติดตั้งจริงต้องใช้)" }
else { Bad "ต้องรันด้วยสิทธิ์ administrator (คลิกขวา PowerShell -> Run as administrator)" }

if (Test-Path $exePath) { Ok "พบไฟล์แอป $exePath" }
else { Bad "ไม่พบ $exePath — คัดลอกชุด deploy มาไว้ที่ $AppPath ก่อน (สร้างด้วย deploy\publish.ps1)" }

$wwwroot = Join-Path $AppPath 'wwwroot\index.html'
if (Test-Path $wwwroot) { Ok "พบหน้าจอ (wwwroot) ในชุดเดียวกัน" }
else { Warn "ไม่พบ wwwroot\index.html — ชุดนี้จะมีแต่ API ไม่มีหน้าจอ" }

$runtimes = & dotnet --list-runtimes 2>$null
if ($LASTEXITCODE -eq 0 -and ($runtimes | Select-String -SimpleMatch 'Microsoft.AspNetCore.App 8.')) {
    Ok "พบ ASP.NET Core Runtime 8"
} else {
    Bad "ไม่พบ ASP.NET Core Runtime 8 — ติดตั้ง .NET 8 Hosting Bundle หรือ ASP.NET Core Runtime จาก https://dotnet.microsoft.com/download/dotnet/8.0"
}

$sqlcmdPath = Get-Sqlcmd
if ($sqlcmdPath) { Ok "พบ sqlcmd ($sqlcmdPath)" }
else { Warn "ไม่พบ sqlcmd — ข้ามการตรวจฐานข้อมูลและงานสำรองข้อมูล (ติดตั้ง SQL Server command line utilities เพื่อใช้ backup-db.ps1)" }

if ($sqlcmdPath) {
    $dbCheck = Invoke-Sql $sqlcmdPath "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$Database') IS NULL THEN 'MISSING' ELSE 'OK' END;"
    if ($dbCheck -eq 0) {
        Ok "เชื่อมต่อ SQL Server ได้ และพบฐานข้อมูล $Database"
    } else {
        Bad "เชื่อมต่อ SQL Server [$SqlServer] หรือหาฐานข้อมูล [$Database] ไม่ได้ — ตรวจว่า service SQL Server รันอยู่ และบัญชีที่ใช้มีสิทธิ์ (ดู deploy\create-sql-login.ps1)"
    }
}

$inUse = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($inUse) { Warn "พอร์ต $Port มีโปรแกรมอื่นใช้อยู่ (PID $($inUse[0].OwningProcess)) — เลือกพอร์ตอื่นด้วย -Port หรือปิดโปรแกรมนั้น" }
else { Ok "พอร์ต $Port ว่าง" }

# path ข้อมูล Express (อ่านจาก appsettings.json ของชุดที่ติดตั้ง)
$baseConfig = Join-Path $AppPath 'appsettings.json'
$expressPath = $null
if (Test-Path $baseConfig) {
    # appsettings.json มีคอมเมนต์ // (ASP.NET อ่านได้ แต่ ConvertFrom-Json ของ PS 5.1 อ่านไม่ได้) -> ตัดออกก่อน
    try {
        $jsonText = (Get-Content $baseConfig | Where-Object { $_.TrimStart() -notlike '//*' }) -join "`n"
        $cfg = $jsonText | ConvertFrom-Json
        $expressPath = $cfg.Import.ExpressBasePath
    } catch {
        Warn "อ่าน appsettings.json ไม่ได้ ($($_.Exception.Message)) — ข้ามการตรวจ path ข้อมูล Express"
    }
    if ($expressPath) {
        if (Test-Path $expressPath) { Ok "เห็น path ข้อมูล Express: $expressPath" }
        else { Warn "เครื่องนี้มองไม่เห็น $expressPath — ถ้าเป็น network share ให้ใช้ UNC (\\เครื่อง\แชร์) และรัน service ด้วยบัญชีที่มีสิทธิ์อ่าน (-ServiceUser)" }
    }
    if ($expressPath -match '^[A-Za-z]:\\$' -and $expressPath -notmatch '^[Cc]:') {
        Warn "ExpressBasePath เป็น mapped drive ($expressPath) — service มองไม่เห็น drive ที่ผู้ใช้ map ไว้ ต้องแก้เป็น UNC ใน appsettings.Production.json"
    }
}

if ($CheckOnly) {
    Section "สรุปผลตรวจ"
    if ($problems.Count -eq 0) { Write-Host "  พร้อมติดตั้ง (ไม่มีรายการที่ขาด)" -ForegroundColor Green }
    else { Write-Host "  ต้องแก้ $($problems.Count) รายการก่อนติดตั้ง" -ForegroundColor Red }
    if ($notes.Count -gt 0) { Write-Host "  มีคำเตือน $($notes.Count) รายการ" -ForegroundColor Yellow }
    return
}

if ($problems.Count -gt 0) {
    throw "ยังติดตั้งไม่ได้ ต้องแก้ก่อน $($problems.Count) รายการ (ดูรายการ [ขาด] ด้านบน)"
}

# ── 2) ค่าตั้ง production ──────────────────────────────────────────────────────
Section "ค่าตั้ง production"
if (Test-Path $prodConfig) {
    Ok "มี appsettings.Production.json อยู่แล้ว — ไม่แก้ทับ (คงกุญแจและรหัสเดิม)"
} else {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $jwtKey = [Convert]::ToBase64String($bytes)

    if ($SqlUser) {
        $conn = "Server=$SqlServer;Database=$Database;User ID=$SqlUser;Password=$SqlPassword;MultipleActiveResultSets=True;TrustServerCertificate=True;"
    } else {
        $conn = "Server=$SqlServer;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"
        Warn "ไม่ได้ระบุ -SqlUser จึงใช้ Windows authentication — บัญชีที่ service รันด้วยต้องมีสิทธิ์ db_owner บน $Database"
    }

    $settings = [ordered]@{
        ConnectionStrings = [ordered]@{ DefaultConnection = $conn }
        Jwt               = [ordered]@{ Key = $jwtKey }
        Hosting           = [ordered]@{ UseHttpsRedirection = $false }
        Cors              = [ordered]@{ AllowedOrigins = @() }
        Logging           = [ordered]@{ File = [ordered]@{ Enabled = $true; RetainedDays = 90 } }
    }
    $settings | ConvertTo-Json -Depth 6 | Out-File $prodConfig -Encoding utf8
    Ok "สร้าง appsettings.Production.json (Jwt:Key สุ่มใหม่ 48 ไบต์, HTTPS redirect ปิดเพราะใช้ HTTP ใน LAN)"
    Warn "ไฟล์นี้มีความลับของระบบ — สำรองไว้ที่ปลอดภัย ถ้าไฟล์หาย ผู้ใช้ทุกคนต้อง login ใหม่"
}

# ── 3) สำรองฐานข้อมูลก่อนอัปเกรด schema ───────────────────────────────────────
Section "สำรองฐานข้อมูลก่อนสตาร์ตครั้งแรก"
if ($SkipBackup) {
    Warn "ข้ามการสำรองข้อมูลตามที่สั่ง (-SkipBackup)"
} elseif (-not $sqlcmdPath) {
    Warn "ไม่มี sqlcmd จึงสำรองไม่ได้ — แนะนำให้สำรอง $Database ด้วยวิธีอื่นก่อนสตาร์ต (ระบบจะอัปเกรด schema เองตอนสตาร์ต)"
} else {
    $backupScript = Join-Path $PSScriptRoot 'backup-db.ps1'
    if (Test-Path $backupScript) {
        $backupArgs = @{ Server = $SqlServer; Database = $Database; BackupDir = $BackupDir }
        if ($SqlUser) { $backupArgs['SqlUser'] = $SqlUser; $backupArgs['SqlPassword'] = $SqlPassword }
        & $backupScript @backupArgs
        Ok "สำรองฐานข้อมูลแล้ว"
    } else {
        Warn "ไม่พบ backup-db.ps1 ข้าง ๆ สคริปต์นี้ — ข้ามการสำรอง"
    }
}

# ── 4) ลงทะเบียน service ──────────────────────────────────────────────────────
Section "ลงทะเบียน Windows service"
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -ne 'Stopped') { Stop-Service $ServiceName -Force }
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Ok "ลบ service เดิมเพื่อลงทะเบียนใหม่"
}

$newServiceArgs = @{
    Name           = $ServiceName
    BinaryPathName = "`"$exePath`""
    DisplayName    = 'JSP Datacenter API'
    Description    = 'ระบบสำนักงานบัญชี JSP Datacenter (API + หน้าจอ)'
    StartupType    = 'Automatic'
}
if ($ServiceUser) {
    $secure = ConvertTo-SecureString $ServicePassword -AsPlainText -Force
    $newServiceArgs['Credential'] = New-Object System.Management.Automation.PSCredential($ServiceUser, $secure)
}
New-Service @newServiceArgs | Out-Null
Ok "สร้าง service $ServiceName"

# environment variable ของ service (services ไม่เห็น env ของผู้ใช้)
$svcKey = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
Set-ItemProperty -Path $svcKey -Name Environment -Value @(
    'ASPNETCORE_ENVIRONMENT=Production',
    "ASPNETCORE_URLS=http://+:$Port"
) -Type MultiString
Ok "ตั้ง ASPNETCORE_ENVIRONMENT=Production และรับ HTTP ที่พอร์ต $Port"

# ล้มแล้วให้ Windows สตาร์ตให้เองภายใน 1 นาที (3 ครั้งแรก) — ระบบล่มตอนดึกจะกลับมาเอง
& sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
Ok "ตั้งให้รีสตาร์ตอัตโนมัติเมื่อ service ล้ม"

# ── 5) firewall ───────────────────────────────────────────────────────────────
Section "เปิดพอร์ตให้เครื่องในวง LAN"
if (Get-NetFirewallRule -DisplayName $firewallRule -ErrorAction SilentlyContinue) {
    Ok "มีกฎ firewall อยู่แล้ว"
} else {
    New-NetFirewallRule -DisplayName $firewallRule -Direction Inbound -Action Allow `
        -Protocol TCP -LocalPort $Port -Profile Domain,Private | Out-Null
    Ok "เปิดพอร์ต TCP $Port (เฉพาะเครือข่าย Domain/Private ไม่เปิดสู่ Public)"
}

# ── 6) งานสำรองข้อมูลรายวัน ───────────────────────────────────────────────────
Section "งานสำรองข้อมูลรายวัน"
if ($SkipBackupTask) {
    Warn "ข้ามการตั้งงานสำรองข้อมูลตามที่สั่ง (-SkipBackupTask)"
} elseif (Get-ScheduledTask -TaskName $backupTaskName -ErrorAction SilentlyContinue) {
    Ok "มีงาน '$backupTaskName' อยู่แล้ว"
} else {
    $backupScript = Join-Path $AppPath 'deploy\backup-db.ps1'
    if (-not (Test-Path $backupScript)) { $backupScript = Join-Path $PSScriptRoot 'backup-db.ps1' }
    $argLine = "-NonInteractive -ExecutionPolicy Bypass -File `"$backupScript`" -Server $SqlServer -Database $Database -BackupDir `"$BackupDir`""
    if ($SqlUser) { $argLine += " -SqlUser $SqlUser -SqlPassword `"$SqlPassword`"" }
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argLine
    $trigger = New-ScheduledTaskTrigger -Daily -At 1:30am
    Register-ScheduledTask -TaskName $backupTaskName -Action $action -Trigger $trigger `
        -RunLevel Highest -User 'SYSTEM' -Description 'สำรองฐานข้อมูล JSP Datacenter รายวัน' | Out-Null
    Ok "ตั้งงานสำรองข้อมูลรายวัน 01:30 (เก็บ 30 วัน)"
}

# ── 7) สตาร์ต + ทดสอบ ────────────────────────────────────────────────────────
Section "สตาร์ตและทดสอบ"
Start-Service $ServiceName
$deadline = (Get-Date).AddSeconds(90)
$ready = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 3
    try {
        $resp = Invoke-WebRequest "http://localhost:$Port/api/v1/auth/login" -Method Post `
            -Body '{"username":"__probe__","password":"__probe__"}' -ContentType 'application/json' `
            -UseBasicParsing -TimeoutSec 5
        $code = $resp.StatusCode
    } catch {
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode } else { $code = 0 }
    }
    # 401 = API ตอบแล้ว (ปฏิเสธผู้ใช้ปลอมตามที่ควร) = ระบบพร้อม
    if ($code -eq 401) { $ready = $true; break }
}

$svc = Get-Service $ServiceName
if ($ready) {
    Ok "API ตอบแล้ว (สถานะ service: $($svc.Status))"
} else {
    Warn "service สถานะ $($svc.Status) แต่ API ยังไม่ตอบใน 90 วินาที — ดู log ที่ $AppPath\logs\ และ Event Viewer"
}

$hostName = $env:COMPUTERNAME
Write-Host @"

================================================================
ติดตั้งเสร็จ — เข้าใช้งานที่:  http://$hostName`:$Port/
   (เครื่องนี้: http://localhost:$Port/)

ขั้นถัดไป
  1. เข้าระบบด้วย admin แล้วเปลี่ยนรหัสผ่านตามที่ระบบบังคับ
     (ถ้ายังใช้รหัสตั้งต้น admin1234 ระบบจะบังคับเปลี่ยนทันทีหลัง login)
  2. ยังไม่ต้องสร้างบัญชีให้คนอื่น — เมื่อพร้อมเพิ่มทีม ไปที่ ระบบ -> ผู้ใช้งานระบบ
  3. ถ้าจะนำเข้าข้อมูล Express จากเครื่องนี้ ตรวจว่า service เห็น path ตาม Import:ExpressBasePath
     (mapped drive ใช้ไม่ได้ ต้องเป็น UNC + บัญชี service ที่มีสิทธิ์อ่าน)

คำสั่งที่ใช้บ่อย
  Get-Service $ServiceName            # ดูสถานะ
  Restart-Service $ServiceName        # รีสตาร์ตหลังอัปเดตรุ่น
  Get-Content "$AppPath\logs\datacenter-$(Get-Date -Format yyyyMMdd).log" -Tail 50
  .\install-service.ps1 -Uninstall    # ถอนการติดตั้ง (ไม่ลบข้อมูล)
================================================================
"@ -ForegroundColor Green
