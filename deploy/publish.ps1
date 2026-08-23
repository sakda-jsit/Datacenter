<#
.SYNOPSIS
    Build ระบบ JSP Datacenter เป็นชุดพร้อม deploy (backend + frontend ในโฟลเดอร์เดียว)

.DESCRIPTION
    - build frontend (Vite) แล้ววางไฟล์นิ่งไว้ที่ wwwroot ของ API → เสิร์ฟจาก process เดียว
      (same-origin: ไม่ต้องตั้ง CORS, ไม่ต้องมี web server แยกสำหรับ frontend)
    - dotnet publish backend เป็น Release
    - ไม่คัดลอก appsettings.Local.json (ความลับของเครื่อง dev) และไม่ทับ appsettings.Production.json
      ที่มีอยู่แล้วในโฟลเดอร์ปลายทาง

.EXAMPLE
    .\deploy\publish.ps1 -OutputPath D:\Datacenter\app
#>
[CmdletBinding()]
param(
    [string]$OutputPath = "D:\Datacenter\app",
    [switch]$SkipFrontend
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot 'src\backend\Datacenter.Api\Datacenter.Api.csproj'
$frontendDir = Join-Path $repoRoot 'src\frontend'
$staging = Join-Path $env:TEMP ("datacenter-publish-" + [Guid]::NewGuid().ToString('N').Substring(0, 8))

Write-Host "== JSP Datacenter · publish ==" -ForegroundColor Cyan
Write-Host "repo:   $repoRoot"
Write-Host "output: $OutputPath"

# ── 1) frontend ────────────────────────────────────────────────────────────────
if (-not $SkipFrontend) {
    Write-Host "`n[1/3] build frontend (Vite)..." -ForegroundColor Cyan
    Push-Location $frontendDir
    try {
        if (Test-Path 'package-lock.json') { npm ci } else { npm install }
        if ($LASTEXITCODE -ne 0) { throw "npm install ล้มเหลว" }
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build ล้มเหลว" }
    } finally { Pop-Location }
} else {
    Write-Host "`n[1/3] ข้าม build frontend (-SkipFrontend)" -ForegroundColor Yellow
}

# ── 2) backend ────────────────────────────────────────────────────────────────
Write-Host "`n[2/3] dotnet publish backend (Release)..." -ForegroundColor Cyan
dotnet publish $apiProject -c Release -o $staging --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish ล้มเหลว" }

# กันความลับของเครื่อง dev ติดไปกับชุด deploy (ซ้ำกับ CopyToPublishDirectory=Never ใน csproj)
Get-ChildItem -Path $staging -Filter 'appsettings.Local.json' -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force

# frontend build → wwwroot
$dist = Join-Path $frontendDir 'dist'
if (Test-Path $dist) {
    $wwwroot = Join-Path $staging 'wwwroot'
    New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
    Copy-Item -Path (Join-Path $dist '*') -Destination $wwwroot -Recurse -Force
    Write-Host "  วางไฟล์ frontend ที่ wwwroot แล้ว"
} else {
    Write-Host "  [คำเตือน] ไม่พบ $dist — ชุดนี้จะมีแต่ API" -ForegroundColor Yellow
}

# สคริปต์ปฏิบัติการ (ติดตั้ง service / สำรองข้อมูล / ตัวอย่างค่าตั้ง) — ให้ชุด deploy พึ่งตัวเองได้
$deployDir = Join-Path $staging 'deploy'
New-Item -ItemType Directory -Force -Path $deployDir | Out-Null
Get-ChildItem -Path $PSScriptRoot -File |
    Where-Object { $_.Name -ne 'publish.ps1' } |
    Copy-Item -Destination $deployDir -Force
Write-Host "  ใส่สคริปต์ปฏิบัติการไว้ที่ deploy\ ในชุดแล้ว"

# ── 3) คัดลอกไปโฟลเดอร์ปลายทาง (คง appsettings.Production.json เดิม) ───────────
Write-Host "`n[3/3] คัดลอกไป $OutputPath ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

$keepProd = Join-Path $OutputPath 'appsettings.Production.json'
$prodBackup = $null
if (Test-Path $keepProd) {
    $prodBackup = Join-Path $env:TEMP ('appsettings.Production.' + (Get-Date -Format 'yyyyMMddHHmmss') + '.json')
    Copy-Item $keepProd $prodBackup -Force
    Write-Host "  สำรอง appsettings.Production.json เดิมไว้ที่ $prodBackup"
}

Copy-Item -Path (Join-Path $staging '*') -Destination $OutputPath -Recurse -Force
if ($prodBackup) { Copy-Item $prodBackup $keepProd -Force }
Remove-Item $staging -Recurse -Force

Write-Host "`nเสร็จแล้ว" -ForegroundColor Green
Write-Host @"

ขั้นถัดไป — คัดลอกโฟลเดอร์นี้ทั้งชุดไปเครื่อง server แล้วรันในโฟลเดอร์ deploy\ ของชุด:

  1. ตรวจความพร้อม (ไม่แก้อะไร):
       .\install-service.ps1 -AppPath <โฟลเดอร์ชุด> -CheckOnly
  2. สร้าง SQL login ให้ระบบ (ครั้งเดียว, ใช้บัญชี sysadmin ของ SQL Server):
       .\create-sql-login.ps1 -Database DatacenterDb -Login dc_app -Password '<รหัสสุ่ม>'
  3. ติดตั้ง (PowerShell แบบ Run as administrator):
       .\install-service.ps1 -AppPath <โฟลเดอร์ชุด> -Port 5000 -SqlUser dc_app -SqlPassword '<รหัสเดิม>'
     สคริปต์จะสร้าง appsettings.Production.json (สุ่ม Jwt:Key), สำรอง DB, ลงทะเบียน service,
     เปิด firewall, ตั้งงานสำรองรายวัน แล้วทดสอบว่า API ตอบจริง
  4. เข้าระบบด้วย admin → ระบบบังคับเปลี่ยนรหัสทันที (รายละเอียด: deploy\README.md)

  หมายเหตุ: ถ้าจะนำเข้า Express จากเครื่อง server ต้องตั้ง Import:ExpressBasePath เป็น UNC
  (mapped drive ใช้ไม่ได้) และรัน service ด้วยบัญชีที่อ่าน share นั้นได้
"@
