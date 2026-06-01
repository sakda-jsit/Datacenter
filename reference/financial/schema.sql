/* =====================================================================
   JSP CONNX - Financial Reporting Web App
   SQL Server schema - Phase 2 (งบการเงิน)
   ฐานข้อมูลปลายทางที่รับข้อมูลจาก Express ผ่านปุ่ม Sync
   ===================================================================== */

/* ---------------------------------------------------------------
   1) บันทึกการ Sync แต่ละครั้ง (กดปุ่ม Sync 1 ครั้ง = 1 แถว)
   --------------------------------------------------------------- */
CREATE TABLE sync_run (
    sync_run_id   BIGINT IDENTITY(1,1) PRIMARY KEY,
    started_at    DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    finished_at   DATETIME2     NULL,
    fiscal_year   INT           NOT NULL,              -- ปีบัญชี เช่น 2568
    source        NVARCHAR(50)  NOT NULL DEFAULT N'EXPRESS',
    status        NVARCHAR(20)  NOT NULL DEFAULT N'RUNNING', -- RUNNING/SUCCESS/FAILED
    row_count     INT           NULL,
    message       NVARCHAR(500) NULL,
    run_by        NVARCHAR(100) NULL
);

/* ---------------------------------------------------------------
   2) STAGING - งบทดลองดิบจาก Express (โครงตรงกับรายงานของ Express)
      โหลดทับทั้งชุดต่อ (fiscal_year) ทุกครั้งที่ Sync
   --------------------------------------------------------------- */
CREATE TABLE stg_trial_balance (
    sync_run_id   BIGINT        NOT NULL REFERENCES sync_run(sync_run_id),
    fiscal_year   INT           NOT NULL,
    account_code  NVARCHAR(20)  NOT NULL,
    account_name  NVARCHAR(150) NULL,
    bf_debit      DECIMAL(18,2) NOT NULL DEFAULT 0,   -- ยอดยกมา
    bf_credit     DECIMAL(18,2) NOT NULL DEFAULT 0,
    period_debit  DECIMAL(18,2) NOT NULL DEFAULT 0,   -- ระหว่างงวด
    period_credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    bal_debit     DECIMAL(18,2) NOT NULL DEFAULT 0,   -- ยอดคงเหลือ (หลังปรับปรุง)
    bal_credit    DECIMAL(18,2) NOT NULL DEFAULT 0,
    note          NVARCHAR(200) NULL
);
CREATE INDEX ix_stg_tb ON stg_trial_balance(fiscal_year, account_code);

/* ---------------------------------------------------------------
   3) CORE - ผังบัญชี + การ map เข้าบรรทัดงบการเงิน
   --------------------------------------------------------------- */

-- นิยามบรรทัดในงบ (REF) เช่น A1=เงินสด, I1=รายได้ขาย  (seed จาก seed_statement_mapping.sql)
CREATE TABLE statement_line (
    ref_code    NVARCHAR(10)  NOT NULL PRIMARY KEY,
    line_name   NVARCHAR(200) NOT NULL,
    section     CHAR(1)       NOT NULL,   -- A=สินทรัพย์ L=หนี้สิน E=ทุน I=รายได้ X=ค่าใช้จ่าย
    sort_order  INT           NOT NULL
);

-- เลขบัญชี -> REF  (seed จาก seed_statement_mapping.sql)
CREATE TABLE account_mapping (
    account_code NVARCHAR(20)  NOT NULL PRIMARY KEY,
    account_name NVARCHAR(150) NULL,
    ref_code     NVARCHAR(10)  NOT NULL REFERENCES statement_line(ref_code)
);

-- งบทดลองที่ผ่านการ clean แล้ว (1 บัญชี/ปี) ใช้คำนวณงบ
CREATE TABLE trial_balance (
    fiscal_year   INT           NOT NULL,
    account_code  NVARCHAR(20)  NOT NULL,
    bf_net        DECIMAL(18,2) NOT NULL DEFAULT 0,  -- ยอดยกมา (เดบิต - เครดิต)
    balance_net   DECIMAL(18,2) NOT NULL DEFAULT 0,  -- ยอดคงเหลือ (เดบิต - เครดิต)
    loaded_at     DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    sync_run_id   BIGINT        NULL REFERENCES sync_run(sync_run_id),
    CONSTRAINT pk_trial_balance PRIMARY KEY (fiscal_year, account_code)
);

/* ---------------------------------------------------------------
   4) Input ที่มาจากนอกงบทดลอง (เช่น ภาษีเงินได้นิติบุคคล จากชีต TAX/ภงด.50)
      งบกำไรขาดทุนใช้บรรทัดนี้เป็น X4 ภาษีเงินได้
   --------------------------------------------------------------- */
CREATE TABLE fs_external_input (
    fiscal_year INT           NOT NULL,
    ref_code    NVARCHAR(10)  NOT NULL,   -- เช่น X4
    amount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    note        NVARCHAR(200) NULL,
    CONSTRAINT pk_fs_external PRIMARY KEY (fiscal_year, ref_code)
);

/* ---------------------------------------------------------------
   5) ผู้ใช้ + สิทธิ์ (Maker / Checker ~10 คน)
   --------------------------------------------------------------- */
CREATE TABLE app_role (
    role_code NVARCHAR(20)  NOT NULL PRIMARY KEY,   -- MAKER / CHECKER / ADMIN
    role_name NVARCHAR(50)  NOT NULL
);
CREATE TABLE app_user (
    user_id    INT IDENTITY(1,1) PRIMARY KEY,
    username   NVARCHAR(100) NOT NULL UNIQUE,
    full_name  NVARCHAR(150) NULL,
    role_code  NVARCHAR(20)  NOT NULL REFERENCES app_role(role_code),
    is_active  BIT           NOT NULL DEFAULT 1,
    created_at DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
CREATE TABLE audit_log (
    audit_id  BIGINT IDENTITY(1,1) PRIMARY KEY,
    at        DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    username  NVARCHAR(100) NULL,
    action    NVARCHAR(100) NOT NULL,   -- LOGIN / SYNC / VIEW_FS / EXPORT / CLOSE_PERIOD
    detail    NVARCHAR(500) NULL
);

/* ---------------------------------------------------------------
   6) VIEW - งบการเงินคำนวณสด (สินทรัพย์/หนี้สิน/ทุน + รายได้/ค่าใช้จ่าย)
      เครื่องหมาย: สินทรัพย์/ค่าใช้จ่าย = เดบิตบวก ; หนี้สิน/ทุน/รายได้ = พลิกเป็นบวก
      หมายเหตุ: บรรทัด RE และ X4 จัดการในชั้น app/รายงาน (ดูเอกสารแนวทาง)
   --------------------------------------------------------------- */
GO
CREATE VIEW v_statement_line_amount AS
SELECT
    tb.fiscal_year,
    sl.ref_code,
    sl.line_name,
    sl.section,
    sl.sort_order,
    CASE WHEN sl.section IN ('L','E','I')        -- หนี้สิน/ทุน/รายได้ -> พลิกด้าน
         THEN -SUM(tb.balance_net)
         ELSE  SUM(tb.balance_net)
    END AS amount
FROM trial_balance tb
JOIN account_mapping am ON am.account_code = tb.account_code
JOIN statement_line  sl ON sl.ref_code     = am.ref_code
GROUP BY tb.fiscal_year, sl.ref_code, sl.line_name, sl.section, sl.sort_order;
GO

/* seed ผู้ใช้/บทบาทเริ่มต้น */
INSERT INTO app_role(role_code, role_name) VALUES
 (N'MAKER',   N'ผู้จัดทำ'),
 (N'CHECKER', N'ผู้สอบทาน/อนุมัติ'),
 (N'ADMIN',   N'ผู้ดูแลระบบ');
