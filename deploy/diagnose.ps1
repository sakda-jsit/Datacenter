<#
.SYNOPSIS
    รวบรวมสาเหตุที่ API สตาร์ตไม่ขึ้น (500.30 / service ไม่ start) ในคำสั่งเดียว

.DESCRIPTION
    อ่านอย่างเดียว ไม่แก้อะไรในระบบ — เอาผลลัพธ์ทั้งหมดไปวิเคราะห์ต่อได้เลย
    ครอบ 6 จุดที่เป็นสาเหตุจริงเกือบทุกครั้ง: runtime, ค่าตั้ง, สิทธิ์เขียน log,
    stdout log ของ ANCM, event log, และผลการรันแอปตรง ๆ

.EXAMPLE
    .\diagnose.ps1 -AppPath D:\WEB\DATACENTER\API
#>
[CmdletBinding()]
param(
    [string]$AppPath = (Split-Path -Parent $PSScriptRoot),
    [int]$RunSeconds = 25
)

function Head($t) { Write-Host "`n=== $t ===" -ForegroundColor Cyan }
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

Head "1) runtime ที่ติดตั้ง (ต้องมี Microsoft.AspNetCore.App 8.x)"
try { & dotnet --list-runtimes | Where-Object { $_ -match '^Microsoft\.(AspNet)?Core' } }
catch { Write-Host "ไม่พบคำสั่ง dotnet ใน PATH ของ session นี้ (เปิด PowerShell ใหม่หลังลง Hosting Bundle)" -ForegroundColor Yellow }
$ancm = "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
"ANCM v2 (สำหรับ host ใต้ IIS): $(Test-Path $ancm)"

Head "2) ไฟล์ค่าตั้ง (แสดงว่ามีค่าหรือไม่ ไม่แสดงค่าจริง)"
$prod = Join-Path $AppPath 'appsettings.Production.json'
if (-not (Test-Path $prod)) {
    Write-Host "ไม่พบ appsettings.Production.json — แอปจะไม่มี connection string/Jwt:Key แล้วล้มทันที" -ForegroundColor Red
} else {
    $raw = Get-Content $prod -Raw -Encoding UTF8
    # ตัด comment ออกก่อน parse (ไฟล์นี้มี comment แบบ // ซึ่ง ConvertFrom-Json ไม่รับ)
    $clean = ($raw -split "`n" | Where-Object { $_.TrimStart() -notlike '//*' }) -join "`n"
    try {
        $cfg = $clean | ConvertFrom-Json
        $cs = [string]$cfg.ConnectionStrings.DefaultConnection
        "ConnectionStrings:DefaultConnection : $(if ($cs) { 'มีค่า' } else { 'ว่าง (ปัญหา)' })"
        if ($cs) { "  server ที่ระบุ : " + (($cs -split ';' | Where-Object { $_ -match '^\s*Server\s*=' }) -join '') }
        "Jwt:Key                            : $(if ($cfg.Jwt.Key) { 'มีค่า (' + $cfg.Jwt.Key.Length + ' ตัวอักษร)' } else { 'ว่าง (ปัญหา)' })"
        "Cors:AllowedOrigins                : " + ($cfg.Cors.AllowedOrigins -join ', ')
        "Import:ExpressBasePath             : " + $cfg.Import.ExpressBasePath +
            " (เข้าถึงได้: $(Test-Path $cfg.Import.ExpressBasePath))"
        if ($cs -match 'Password=<') { Write-Host "  รหัสยังเป็น placeholder <...> — ต่อฐานไม่ได้" -ForegroundColor Red }
    } catch { Write-Host "อ่าน JSON ไม่ได้ (ไฟล์อาจเสีย): $($_.Exception.Message)" -ForegroundColor Red }
}
"ASPNETCORE_ENVIRONMENT (ของ session นี้) : $($env:ASPNETCORE_ENVIRONMENT)"

Head "3) สิทธิ์เขียนโฟลเดอร์ log (แอปเขียน log ตอนสตาร์ต ถ้าเขียนไม่ได้จะล้ม)"
$logDir = Join-Path $AppPath 'logs'
"โฟลเดอร์ logs มีอยู่ : $(Test-Path $logDir)"
try {
    $probe = Join-Path $AppPath ('write-probe-' + [Guid]::NewGuid().ToString('N').Substring(0,6) + '.tmp')
    [IO.File]::WriteAllText($probe, 'probe'); Remove-Item $probe -Force
    "เขียนไฟล์ในโฟลเดอร์แอปได้ (ด้วยบัญชีที่รันสคริปต์นี้) : True"
} catch { Write-Host "เขียนไฟล์ในโฟลเดอร์แอปไม่ได้: $($_.Exception.Message)" -ForegroundColor Yellow }
if (Test-Path $logDir) { (Get-Acl $logDir).Access |
    Where-Object { $_.IdentityReference -match 'IIS|IUSR|NETWORK SERVICE|Users' } |
    Select-Object IdentityReference, FileSystemRights | Format-Table -AutoSize | Out-String -Width 120 }

Head "4) stdout log ของ ANCM (ต้องเปิด stdoutLogEnabled=true ใน web.config)"
$std = Get-ChildItem (Join-Path $logDir 'stdout*.log') -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($std) { "ไฟล์: $($std.FullName)  ($($std.LastWriteTime))"; Get-Content $std.FullName -Tail 40 }
else { Write-Host "ไม่พบ stdout log — ตรวจว่า web.config มี stdoutLogEnabled=`"true`" และ app pool เขียนโฟลเดอร์ logs ได้" -ForegroundColor Yellow }

Head "5) log ของแอปเอง (Logging:File)"
$appLog = Get-ChildItem (Join-Path $logDir 'datacenter-*.log') -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($appLog) { "ไฟล์: $($appLog.Name)"; Get-Content $appLog.FullName -Tail 15 } else { "ไม่พบ" }

Head "6) Event log ที่เกี่ยวข้อง (10 รายการล่าสุด)"
try {
    Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = (Get-Date).AddHours(-6) } -ErrorAction Stop |
        Where-Object { $_.ProviderName -match 'IIS AspNetCore Module|\.NET Runtime|Application Error' } |
        Select-Object -First 10 TimeCreated, ProviderName, @{n='Message';e={ ($_.Message -split "`n")[0..2] -join ' ' }} |
        Format-List | Out-String -Width 200
} catch { "อ่าน event log ไม่ได้: $($_.Exception.Message)" }

Head "7) ทดลองรันแอปตรง ๆ $RunSeconds วินาที (จับ exception จริง)"
$exe = Join-Path $AppPath 'Datacenter.Api.exe'
if (-not (Test-Path $exe)) { Write-Host "ไม่พบ $exe" -ForegroundColor Red; return }
$o = Join-Path $env:TEMP 'dc-diag-out.log'; $e = Join-Path $env:TEMP 'dc-diag-err.log'
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$t0 = Get-Date
$p = Start-Process -FilePath $exe -WorkingDirectory $AppPath -RedirectStandardOutput $o -RedirectStandardError $e -PassThru -WindowStyle Hidden
# รอจนแอปตายเองหรือหมดเวลา (WaitForExit คืน false เมื่อหมดเวลาโดยที่ยังรันอยู่)
$exited = $p.WaitForExit($RunSeconds * 1000)
if (-not $exited) { Stop-Process -Id $p.Id -Force; $p.WaitForExit(3000) | Out-Null }
# รอให้ไฟล์ที่ redirect ไว้ถูก flush ปิดจริง ไม่งั้นอ่านได้ไฟล์ว่างแม้แอปจะล้มไปแล้ว
Start-Sleep -Milliseconds 1200
$err = if (Test-Path $e) { Get-Content $e -Raw -Encoding UTF8 } else { '' }
Write-Host "-- stderr --" -ForegroundColor Yellow
if ($err) { ($err -split "`r?`n" | Select-Object -First 12) -join [Environment]::NewLine } else { "(ว่าง)" }
Write-Host "-- stdout (ท้าย) --" -ForegroundColor Yellow
if (Test-Path $o) { Get-Content $o -Encoding UTF8 | Select-Object -Last 8 } else { "(ว่าง)" }

# ── สรุปวินิจฉัย ───────────────────────────────────────────────────────────────
# ยึด stderr เป็นหลัก ไม่ใช่ว่า process ยัง alive อยู่หรือไม่: SqlClient วน retry
# ทำให้ process ค้างอยู่ได้อีกพักหนึ่งแม้จะโยน unhandled exception ไปแล้ว
Head "สรุป"
$verdict = @()
if ($err -match 'error: 26|error: 40|Error Locating Server|network-related') {
    $verdict += "ต่อ SQL Server ไม่ได้ — instance ที่ระบุใน connection string ยังไม่มีหรือยังเข้าถึงไม่ได้"
    $verdict += "  แก้: ลง SQL Server, เปิด TCP/IP + SQL Server Browser, ตรวจชื่อ instance, เปิด firewall UDP 1434"
}
if ($err -match 'Login failed for user') {
    $verdict += "ต่อ SQL ถึงแล้วแต่ login ไม่ผ่าน — รหัสไม่ตรง หรือ SQL ยังไม่เปิด Mixed Mode authentication"
    $verdict += "  แก้: ALTER LOGIN ให้ตรงกับรหัสใน appsettings.Production.json แล้ว restart SQL service"
}
if ($err -match 'Cannot open database|database .* does not exist') {
    $verdict += "ยังไม่มีฐานข้อมูล — สร้างก่อน: CREATE DATABASE DatacenterDb (dc_app ไม่มีสิทธิ์สร้างฐาน)"
}
if ($err -match 'ค่าตั้งระบบไม่ครบ|InvalidOperationException.*Jwt') {
    $verdict += "ค่าตั้งไม่ครบ — แอปไม่ได้อ่าน appsettings.Production.json (ตรวจ ASPNETCORE_ENVIRONMENT=Production) หรือค่าว่าง"
}
if ($err -match 'UnauthorizedAccessException|Access to the path') {
    $verdict += "สิทธิ์ไฟล์ไม่พอ — บัญชีที่รันแอปเขียนโฟลเดอร์ logs/ไฟล์แนบไม่ได้"
}
# ถ้า stderr ว่าง ให้ยืนยันด้วย event log ว่าแอปล้มระหว่างการทดสอบนี้หรือไม่
$crashed = $false
if (-not $err) {
    try {
        $crashed = [bool](Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $t0 } -ErrorAction Stop |
            Where-Object { $_.ProviderName -match 'Application Error|\.NET Runtime' -and $_.Message -match 'Datacenter\.Api' } |
            Select-Object -First 1)
    } catch { }
}
if ($exited -or $crashed) {
    if (-not $err) {
        $verdict += "แอปล้มระหว่างสตาร์ต แต่ไม่มีข้อความใน stderr — ดูรายละเอียดใน Event Viewer (Windows Logs > Application)"
        $verdict += "  ค้นด้วย: Get-WinEvent -LogName Application -MaxEvents 30 | ? Message -match 'Datacenter' | fl TimeCreated,Message"
    }
} elseif (-not $err) {
    $verdict += "แอปสตาร์ตได้เองตามปกติ — ถ้าใต้ IIS ยังล้ม ปัญหาอยู่ที่ระดับ IIS: app pool ต้องเป็น No Managed Code,"
    $verdict += "  ต้องมี ANCM v2 (ดูข้อ 1) และ IIS AppPool\<ชื่อ pool> ต้องเขียนโฟลเดอร์ logs ได้"
}
if ($verdict.Count -eq 0) { $verdict += "ไม่เข้าเคสที่รู้จัก — ส่งผลลัพธ์ทั้งหมดนี้ให้นักพัฒนาดู" }
$verdict | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }

Write-Host "`nเสร็จ — คัดลอกผลทั้งหมดนี้ส่งให้ผู้ดูแลระบบ/นักพัฒนาดูได้เลย" -ForegroundColor Green
