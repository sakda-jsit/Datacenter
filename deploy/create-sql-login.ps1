<#
.SYNOPSIS
    สร้าง SQL login เฉพาะงานให้ระบบใช้ (แทนการใช้ sa)

.DESCRIPTION
    ระบบต้องมีสิทธิ์สร้าง/แก้ตารางในฐานข้อมูลของตัวเอง (รัน EF migrations ตอนสตาร์ต)
    จึงให้เป็น db_owner ของ DatacenterDb "เฉพาะฐานนี้" ไม่ต้องมีสิทธิ์ระดับ server ใด ๆ

    รันด้วยบัญชีที่เป็น sysadmin ของ SQL Server (Windows auth ของผู้ดูแลเครื่อง หรือระบุ -AdminUser sa)
    เขียนให้รองรับ Windows PowerShell 5.1

.EXAMPLE
    .\create-sql-login.ps1 -Login dc_app -Password 'รหัสยาว ๆ ที่สุ่มมา'

.EXAMPLE
    # ใช้บัญชี sa ของ SQL Server เป็นผู้สร้าง (กรณี Windows auth ไม่ใช่ sysadmin)
    .\create-sql-login.ps1 -Login dc_app -Password '***' -AdminUser sa -AdminPassword '***'
#>
[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'DatacenterDb',
    [string]$Login = 'dc_app',
    [Parameter(Mandatory = $true)][string]$Password,
    [string]$AdminUser,
    [string]$AdminPassword
)

$ErrorActionPreference = 'Stop'

$sqlcmd = $null
$cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($cmd) { $sqlcmd = $cmd.Source }
if (-not $sqlcmd) {
    $candidate = Get-ChildItem 'C:\Program Files\Microsoft SQL Server\*\Tools\Binn\sqlcmd.exe',
                               'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\*\Tools\Binn\sqlcmd.exe' `
                               -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $candidate) { throw "ไม่พบ sqlcmd — ติดตั้ง SQL Server command line utilities ก่อน" }
    $sqlcmd = $candidate.FullName
}

if ($Password.Length -lt 12) { throw "รหัสของ login ควรยาวอย่างน้อย 12 ตัวอักษร" }
if ($Login -notmatch '^[A-Za-z][A-Za-z0-9_]{2,30}$') { throw "ชื่อ login ใช้ได้เฉพาะ a-z 0-9 _ (เริ่มด้วยตัวอักษร)" }

$auth = @('-E')
if ($AdminUser) { $auth = @('-U', $AdminUser, '-P', $AdminPassword) }

# escape เครื่องหมาย ' ในรหัสผ่านสำหรับ T-SQL
$pwdEscaped = $Password.Replace("'", "''")

$sql = @"
SET NOCOUNT ON;
IF DB_ID('$Database') IS NULL
    THROW 50000, 'ไม่พบฐานข้อมูลที่ระบุ', 1;

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '$Login')
BEGIN
    CREATE LOGIN [$Login] WITH PASSWORD = N'$pwdEscaped',
        DEFAULT_DATABASE = [$Database], CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
    PRINT 'สร้าง login แล้ว';
END
ELSE
    PRINT 'มี login นี้อยู่แล้ว (ไม่เปลี่ยนรหัส)';

USE [$Database];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$Login')
BEGIN
    CREATE USER [$Login] FOR LOGIN [$Login];
    PRINT 'สร้าง user ในฐานข้อมูลแล้ว';
END
ELSE
    PRINT 'มี user ในฐานข้อมูลนี้อยู่แล้ว';

ALTER ROLE db_owner ADD MEMBER [$Login];
PRINT 'ให้สิทธิ์ db_owner เฉพาะฐาน $Database แล้ว';
"@

Write-Host "สร้าง/ตรวจ login [$Login] บน [$Server] สำหรับฐาน [$Database]" -ForegroundColor Cyan
& $sqlcmd -S $Server @auth -C -b -Q $sql
if ($LASTEXITCODE -ne 0) { throw "สร้าง login ไม่สำเร็จ (exit $LASTEXITCODE)" }

# ตรวจว่า login ใหม่เชื่อมต่อได้จริง
& $sqlcmd -S $Server -U $Login -P $Password -d $Database -C -b -h -1 -W -Q "SELECT 'เชื่อมต่อด้วย $Login ได้ และเห็นตาราง ' + CAST(COUNT(*) AS varchar(10)) FROM sys.tables;"
if ($LASTEXITCODE -ne 0) { throw "login สร้างแล้วแต่เชื่อมต่อไม่ได้ — ตรวจว่า SQL Server เปิด SQL authentication (Mixed Mode)" }

Write-Host "`nเสร็จแล้ว — ใช้ค่านี้ใน appsettings.Production.json:" -ForegroundColor Green
Write-Host "  Server=$Server;Database=$Database;User ID=$Login;Password=<รหัสที่ตั้ง>;MultipleActiveResultSets=True;TrustServerCertificate=True;"
Write-Host "หรือส่งให้ install-service.ps1 ด้วย -SqlUser $Login -SqlPassword '<รหัส>'"
