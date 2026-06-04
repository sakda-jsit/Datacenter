# Modules

## 1. Client Management
Manage accounting clients, company information, tax ID, fiscal years, branches, and assigned users.

## 2. Import Data
Import master data and transactions from Express Accounting, DBF, Excel, and CSV.

## 3. VAT Management
Manage input VAT, output VAT, VAT validation, and VAT reconciliation.

## 4. AR Management
Manage customers, invoices, receipts, outstanding balances, and AR aging.

## 5. AP Management
Manage suppliers, bills, payments, outstanding balances, and AP aging.

## 6. Payroll
Manage employee master, salary calculation, social security, withholding tax, payslips, and payroll journal entries.

## 7. Bank Reconciliation
Import bank statements and match them with accounting transactions.

## 8. Trial Balance
Generate opening balance, period movement, and ending balance.

## 9. General Ledger
Provide journal entries, account movement, and ledger inquiry.

## 10. Financial Statement
Generate balance sheet, profit and loss, and cash flow statement.

## 11. Tax Report
Generate PP30, PND1, PND3, PND53, and withholding tax reports.

## 12. Compliance Calendar
Track recurring monthly accounting obligations and filing deadlines.

## 13. Closing Period
Validate, close, lock, and reopen accounting periods with authorization.

## 14. Audit Log
Track login, import, changes, approvals, and critical user actions.

## 15. Dashboard & KPI
Monitor client status, filing progress, overdue tasks, reconciliation status, and monthly closing progress.

---

# โมดูลเพิ่มเติม (Requirement v11 — จาก workbook 2025_JSPC_FIN.xlsx)

โมดูลกลุ่มนี้สกัดจาก workbook ปิดงบจริงของ JSP CONNEX และผ่านการยืนยันคำตอบ User แล้ว
(รายละเอียด business rule/calculation อยู่ในไฟล์ spec ใหม่ docs/13–21)

## 16. Adjusted Trial Balance & Adjustment Entry
งบทดลองหลังปรับปรุง (อ้างอิง sheet `TB25`/`TB24`): คอลัมน์ ยอดยกมา (B/F), เคลื่อนไหวระหว่างงวด,
ยอดคงเหลือก่อนปรับปรุง, รายการปรับปรุง (Adj debit/credit), ยอดหลังปรับปรุง (final debit/credit)
- adjustment ต้องระบุ source type (Leasing/Loan/Manual/Tax/Other) + reference + เหตุผล
- ตรวจ debit = credit ทั้งก่อนและหลังปรับปรุง
- ดู docs/13

## 17. Leasing / Loan Working Paper
หน้าจัดการสัญญาเช่า/เงินกู้ภายในระบบ (อ้างอิงไฟล์ `2025_JSPC_LEASING.xlsx`, `2025_CRUVE_LOAN.xlsx`):
ผู้ใช้คำนวณ schedule ในระบบ เมื่อเสร็จระบบนำผลไปสร้าง adjustment เข้า TB ปีปัจจุบันอัตโนมัติ
- เก็บไฟล์ต้นทาง/หลักฐานประกอบ
- ดู docs/13

## 18. Financial Statement (ส่วนขยาย) — CAP & Notes
- งบแสดงการเปลี่ยนแปลงส่วนของผู้ถือหุ้น (`CAP`)
- หมายเหตุประกอบงบการเงิน (`NOTE2`): แยก template text/form (User แก้ได้เมื่อมาตรฐานบัญชีเปลี่ยน)
  ออกจาก data binding (ดึงจาก TB ปีปัจจุบัน/ปีก่อน); `OLE_LINK` = report header เท่านั้น
- รหัสกลุ่มงบมาตรฐานกรมพัฒนาธุรกิจการค้า (`npae_com-oth_...xls`) เป็น master taxonomy
- รายงานต้องตรง Excel เดิม 100% ตาม Page Break Preview
- ดู docs/13

## 19. Fixed Asset Register
ทะเบียนสินทรัพย์หลัก (`FA` + สรุป `SUM`):
- ค่าเสื่อมราคา 2 ชุด (บัญชี + ภาษี) แยกกันชัดเจน
- master อัตราค่าเสื่อม/อายุการใช้งานต่อประเภทสินทรัพย์ (default + override)
- รองรับซื้อเพิ่ม/จำหน่าย/ขาย/ตัดจำหน่าย + คำนวณกำไร/ขาดทุนจากการขายอัตโนมัติ
- ดู docs/14

## 20. Prepaid Expense Schedule
ตัดจ่ายค่าใช้จ่ายล่วงหน้า (`PREPAID`, `PREPAID ANTIVIRUS`) ด้วย pattern เดียวเป็นมาตรฐานกลาง
- ดู docs/14

## 21. Stock / Inventory
สินค้าคงเหลือ + ต้นทุน (`STOCK2025/2024`, `FG2025`):
- ต้นทุน FIFO จาก Express, รองรับหลายคลัง, ใช้คำนวณต้นทุน ไม่ใช่แค่รายงาน
- `FG` reconcile กับ TB ปีปัจจุบัน — กรณีต่าง: แสดงผลต่าง ให้บัญชีบันทึก adjustment เอง (ไม่ auto)
- ดู docs/15

## 22. Cash Count & Interest Income
- กระดาษทำการตรวจนับเงินสด (`CASH COUNT`) + แนบ bank evidence
- ดอกเบี้ยเงินให้กู้กรรมการ (`INTEREST INCOME`, `22120`): เงินต้น×อัตรา×วัน/ฐานปี + ภาษีธุรกิจเฉพาะ
- ดู docs/13, docs/17

## 23. AR/AP Reconciliation + Bank Statement
จับคู่ `RE`→`AR-RECEIPT` และ `PV`→`AP-PAYMENT` กับ bank statement
- WHT + bank charge ดึงจาก Express; status `matched` / `partial` / `unmatched`
- ดู docs/17

## 24. Subsequent Payment Check
ใช้ข้อมูลปีถัดไป (`GL1`/`JV1`) ตรวจว่ารายการค้างจ่ายปีปิดงบจ่ายชำระแล้วหรือยัง
(reference เท่านั้น — ไม่นำมารวมยอดปีปิดงบ)
- ดู docs/17

## 25. Tax (ส่วนขยาย) — TAX engine / PP30 / PND PDF
- `TAX`: ระบบคำนวณภาษีเงินได้นิติบุคคลทั้งหมดจาก TB + รายการปรับปรุง (ต่อยอดจาก ภ.ง.ด.50 ที่มีแล้ว)
- `PP.30`: ดึง Input/Output VAT อัตโนมัติ, balance carry-forward รายเดือน
- `PND.3/53`: upload PDF จากสรรพากร (layout คงที่ → template parser) → reconcile กับ Express → เก็บเฉพาะที่ตรง
- รองรับเบี้ยปรับ/เงินเพิ่ม + ยื่นเพิ่มเติมไม่จำกัดครั้ง/เดือน
- ดู docs/16

## 26. Control / Audit / Evidence (ส่วนขยาย)
- field-level audit trail ทุก field, attachment/evidence management, audit log export ให้ผู้สอบบัญชี
- report package draft/review/final/lock, import evidence log + snapshot
- ดู docs/18
