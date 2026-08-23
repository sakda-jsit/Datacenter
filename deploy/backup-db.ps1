<#
.SYNOPSIS
    สำรองฐานข้อมูล DatacenterDb (full backup) + ลบไฟล์เก่าเกินกำหนด

.DESCRIPTION
    ฐานข้อมูลนี้เก็บงบการเงินที่ยื่นแล้ว, หลักฐานการนำเข้า (ImportSnapshot) และเอกสารแนบ
    รวมรูปบัตรประชาชนพนักงาน (PDPA) ซึ่งเก็บเป็น blob ในฐานข้อมูล → ไฟล์ backup เป็นข้อมูลส่วนบุคคล
    ต้องเก็บในที่ที่คุมสิทธิ์การเข้าถึง และห้ามวางบน cloud drive ที่แชร์ทั่วองค์กร

    ตั้งเป็นงานประจำวันด้วย Task Scheduler (รัน powershell.exe -File ... -NonInteractive)
    หรือ SQL Server Agent job ก็ได้

.EXAMPLE
    .\deploy\backup-db.ps1 -BackupDir D:\Backup\Datacenter -RetainDays 30
    .\deploy\backup-db.ps1 -SqlUser dc_backup -SqlPassword '***'   # ถ้าไม่ใช้ Windows auth
#>
[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'DatacenterDb',
    [string]$BackupDir = 'D:\Backup\Datacenter',
    [int]$RetainDays = 30,
    [string]$SqlUser,
    [string]$SqlPassword
)

$ErrorActionPreference = 'Stop'

# หมายเหตุ: เขียนให้รองรับ Windows PowerShell 5.1 (ไม่ใช้ ?. / ?? / ternary)
$sqlcmd = $null
$cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($cmd) { $sqlcmd = $cmd.Source }
if (-not $sqlcmd) {
    $candidate = Get-ChildItem 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\sqlcmd.exe' -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $candidate) { throw "ไม่พบ sqlcmd — ติดตั้ง SQL Server command line utilities ก่อน" }
    $sqlcmd = $candidate.FullName
}

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$file = Join-Path $BackupDir "$Database-$stamp.bak"

$query = @"
BACKUP DATABASE [$Database]
TO DISK = N'$file'
WITH INIT, COMPRESSION, CHECKSUM, STATS = 10,
     NAME = N'$Database full backup $stamp';
"@

$auth = @('-E')
if ($SqlUser) { $auth = @('-U', $SqlUser, '-P', $SqlPassword) }

Write-Host "สำรอง [$Database] → $file"
& $sqlcmd -S $Server @auth -C -b -Q $query
if ($LASTEXITCODE -ne 0) { throw "backup ล้มเหลว (exit $LASTEXITCODE)" }

$size = [Math]::Round((Get-Item $file).Length / 1MB, 1)
Write-Host "สำเร็จ: $file ($size MB)" -ForegroundColor Green

# ── ตรวจความสมบูรณ์ของไฟล์ backup (จับไฟล์เสียตั้งแต่วันนี้ ไม่ใช่วันที่ต้องกู้) ──
& $sqlcmd -S $Server @auth -C -b -Q "RESTORE VERIFYONLY FROM DISK = N'$file' WITH CHECKSUM;"
if ($LASTEXITCODE -ne 0) { throw "ไฟล์ backup ตรวจไม่ผ่าน (RESTORE VERIFYONLY) — ห้ามลบไฟล์เก่า" }
Write-Host "ตรวจไฟล์ backup ผ่าน (RESTORE VERIFYONLY)" -ForegroundColor Green

# ── ลบไฟล์เก่าเกินกำหนด ──
if ($RetainDays -gt 0) {
    $cutoff = (Get-Date).AddDays(-$RetainDays)
    $old = Get-ChildItem $BackupDir -Filter "$Database-*.bak" | Where-Object { $_.LastWriteTime -lt $cutoff }
    foreach ($f in $old) {
        Remove-Item $f.FullName -Force
        Write-Host "ลบไฟล์เก่า: $($f.Name)"
    }
}
