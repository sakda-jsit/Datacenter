/* ─────────────────────────────────────────────────────────────────────────────
   สร้างฐานข้อมูลและ SQL login ให้ JSP Datacenter — รันตรงกับ SQL Server
   วิธีรัน: เปิด SSMS ต่อ instance ด้วยบัญชี sysadmin แล้ววางสคริปต์นี้ กด Execute
            หรือ  sqlcmd -S js-server -E -i create-login.sql

   ► ก่อนรัน: แทน <PASSWORD> ทั้ง 2 จุดด้วยรหัสเดียวกับที่อยู่ใน
     appsettings.Production.json (ช่อง Password= ของ DefaultConnection)
   ───────────────────────────────────────────────────────────────────────────── */

/* 1) ฐานข้อมูล — ต้องสร้างด้วยบัญชี sysadmin เพราะ dc_app ไม่มีสิทธิ์ CREATE DATABASE
      (EF Core สร้างได้แค่ตารางข้างในตอนแอปสตาร์ตครั้งแรก) */
IF DB_ID('DatacenterDb') IS NULL
BEGIN
    CREATE DATABASE [DatacenterDb];
    PRINT 'สร้างฐาน DatacenterDb แล้ว';
END
ELSE PRINT 'มีฐาน DatacenterDb อยู่แล้ว';
GO

/* 2) login ระดับ server (ตัวตนสำหรับเข้า SQL Server)
      CHECK_POLICY = ON  → รหัสต้องผ่านนโยบายรหัสของ Windows (ยาว+ซับซ้อน)
      CHECK_EXPIRATION = OFF → รหัสไม่หมดอายุ เพราะเป็นบัญชีของโปรแกรม
                               ถ้าเปิดไว้ วันหมดอายุมาถึงระบบจะล่มเงียบ ๆ */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'dc_app')
BEGIN
    CREATE LOGIN [dc_app] WITH
        PASSWORD = N'<PASSWORD>',
        DEFAULT_DATABASE = [DatacenterDb],
        CHECK_POLICY = ON,
        CHECK_EXPIRATION = OFF;
    PRINT 'สร้าง login dc_app แล้ว';
END
ELSE
BEGIN
    /* มีอยู่แล้ว → ตั้งรหัสใหม่ให้ตรงกับไฟล์ค่าตั้ง */
    ALTER LOGIN [dc_app] WITH PASSWORD = N'<PASSWORD>';
    PRINT 'มี login dc_app อยู่แล้ว — เปลี่ยนรหัสให้ตรงกับไฟล์ค่าตั้งแล้ว';
END
GO

/* 3) user ในฐานข้อมูล + สิทธิ์
      db_owner เฉพาะ DatacenterDb เท่านั้น (ไม่มีสิทธิ์ระดับ server ใด ๆ)
      ต้องเป็น db_owner เพราะแอปรัน EF migrations เอง = สร้าง/แก้ตารางตอนสตาร์ต */
USE [DatacenterDb];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'dc_app')
BEGIN
    CREATE USER [dc_app] FOR LOGIN [dc_app];
    PRINT 'สร้าง user dc_app ในฐานแล้ว';
END
ELSE PRINT 'มี user dc_app ในฐานอยู่แล้ว';

ALTER ROLE [db_owner] ADD MEMBER [dc_app];
PRINT 'ให้สิทธิ์ db_owner เฉพาะฐาน DatacenterDb แล้ว';
GO

/* 4) ตรวจผล — ค่าที่ต้องได้:
      AuthMode = 'Mixed Mode (ใช้ SQL login ได้)'   ถ้าได้ 'Windows only' ต้องไปเปลี่ยนก่อน
                 (SSMS: คลิกขวาที่ instance > Properties > Security > SQL Server and Windows
                  Authentication mode แล้ว restart service ของ SQL)
      IsDisabled = 0, db_owner = 1 */
SELECT
    CASE SERVERPROPERTY('IsIntegratedSecurityOnly')
         WHEN 1 THEN 'Windows only — ต้องเปลี่ยนเป็น Mixed Mode'
         ELSE 'Mixed Mode (ใช้ SQL login ได้)' END           AS AuthMode,
    CAST(SERVERPROPERTY('Edition') AS varchar(50))            AS Edition,
    @@SERVERNAME                                              AS ServerName,
    (SELECT is_disabled FROM sys.server_principals WHERE name = 'dc_app') AS IsDisabled,
    IS_ROLEMEMBER('db_owner', 'dc_app')                       AS IsDbOwner;
GO
