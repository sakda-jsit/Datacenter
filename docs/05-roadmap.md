# Roadmap

## Phase 1 - Accounting Office Platform
- Client Management
- Import Data
- VAT Management
- AR/AP Management
- Payroll
- Bank Reconciliation
- Trial Balance
- General Ledger
- Financial Statement
- Tax Report
- Compliance Calendar
- Closing Period
- Audit Log
- Dashboard & KPI

## Phase 1.x - Closing Workbook Modules (Requirement v11)
สกัดจาก workbook ปิดงบจริง — business rule ครบ ไม่บล็อก DBF spec จึงทำต่อได้:

- Adjusted Trial Balance + Adjustment Entry
- Leasing / Loan Working Paper → adjustment เข้า TB
- Financial Statement ส่วนขยาย: CAP + NOTE2 + DBD group-code taxonomy + Excel-100% layout
- Fixed Asset Register (book + tax depreciation, disposal)
- Prepaid Expense Schedule
- Stock / Inventory (FIFO, multi-warehouse, FG↔TB)
- Cash Count + Interest Income
- AR/AP Reconciliation + Bank Statement
- Subsequent Payment Check
- Tax ส่วนขยาย: TAX engine, PP30 auto, PND.3/53 PDF reconcile
- Control: field-level audit, attachment/evidence, audit log export, report package, snapshot

> **บล็อกด้วย DBF spec** (รอโครงสร้างตารางจริง): VAT/PP30 source, AR/AP source, Stock source, Bank statement
> **Payroll**: เลื่อนจาก Phase 1 มาทำใน Phase นี้/ถัดไป (workbook v11 ไม่ครอบคลุม)
> **Multi-year posting redesign**: เป็นงาน Phase 2 (ปัจจุบัน posting รองรับทีละ 1 ปีงบ/บริษัท)

## Phase 2 - Workflow and Collaboration
- Task workflow
- Document management
- Client portal
- Approval workflow

## Phase 3 - Integration and AI
- E-Tax integration
- Revenue Department integration
- AI accounting assistant
- Automated anomaly detection
