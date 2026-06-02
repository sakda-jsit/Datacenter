# Business Rules

## Multi Company
- Each client company must be isolated.
- Users can only access authorized companies.
- Every transaction must belong to one company and one accounting period.

## Import Rules
- Every import batch must be recorded.
- Duplicate imports are not allowed.
- Cancelled documents must remain in history.
- Imported source document number must be traceable.
- Import errors must be reviewed before posting to production tables.

## VAT Rules
- Input VAT requires a valid tax invoice.
- Output VAT must be calculated from sales transactions.
- VAT report must reconcile with GL.
- VAT transactions must support document date and tax invoice date.

## AR Rules
- Invoice increases AR.
- Receipt decreases AR.
- Credit note decreases AR.
- Outstanding balance cannot be negative unless explicitly allowed by adjustment.

## AP Rules
- Supplier invoice increases AP.
- Payment decreases AP.
- Debit note decreases AP.
- Outstanding balance cannot be negative unless explicitly allowed by adjustment.

## Payroll Rules
- Payroll includes salary, allowance, overtime, bonus, social security, withholding tax, and other deductions.
- Payroll must generate accounting journal entries.
- Payroll reports must support PND1 and social security filing.

## Reconciliation Rules
- Bank statement must reconcile with GL.
- VAT report must reconcile with GL.
- AR/AP subledger must reconcile with GL.
- Unmatched items must remain visible until resolved.

## Closing Rules
- Closed periods cannot be modified.
- Reopen requires authorized user approval.
- Closing validation must check VAT, AR/AP, bank reconciliation, and GL balance.

## Audit Rules
- Store user, datetime, action, entity, before value, and after value.
- Import, edit, delete, approve, close, and reopen actions must be audited.

## Accounting Period Definition (Express ISPRD)
- นิยามรอบบัญชี (งวด + วันสิ้นงวด + สถานะล็อก) เป็น source of truth ดึงจากตาราง Express `ISPRD` ตอน import
- หนึ่งไฟล์ Express นิยาม 2 ปีบัญชี (ปีปัจจุบัน 12 งวด + ปีถัดไป 12 งวด); นิยามในระบบจะ mirror ISPRD ปัจจุบันเสมอ (แทนที่ทั้งหมดของบริษัทตอน import)
- ฟิลด์ `LOCK='Y'` ของ Express = งวดถูกปิด → seed สถานะ Closing Period เป็น "ปิดงวดแล้ว (Closed)" อัตโนมัติ
- **Import ได้เฉพาะปีที่อยู่ในนิยามรอบบัญชี** (ปีที่ ISPRD ครอบคลุม) — ปีอื่นถูก reject
- **ลบข้อมูลที่ import แล้วได้เฉพาะปีที่อยู่ในนิยามรอบบัญชีปัจจุบัน** — ปีที่หลุดออกจากรอบบัญชีเป็นข้อมูลประวัติ ห้ามลบ (ยกเว้นกรณีบริษัทยังไม่มีนิยามรอบบัญชีเลย)

## Posting Rules (Staging → Production)
- Import เขียนลง staging tables ก่อน (StagingAccount/StagingTrialBalance) แล้วจึง "post" เข้าตารางจริง (Account + JournalEntry/Line)
- รายงาน (งบทดลอง/GL/งบการเงิน/ปิดรอบบัญชี) อ่านจากตารางจริง จึงต้อง post ก่อนข้อมูลจึงปรากฏ
- เมื่อ import สำเร็จ (ไม่มี error) ระบบจะ **auto-post อัตโนมัติ**; ถ้า post ล้มเหลว batch ยังถือว่า import สำเร็จและกด Post ซ้ำได้
- Post ปีเดิมซ้ำ = แทนที่ของเดิม (idempotent ตาม DocumentNo `OPEN-{ปี}`/`MOVE-{ปี}`)
- Import ปีเดิมซ้ำ = แทนที่ batch เดิมของปีนั้นทั้งหมด (1 บริษัท/ปี เหลือ batch เดียว)

> ข้อจำกัด: Express export เป็นยอดรวมรายปี ยอดเคลื่อนไหวถูกลงที่วันสิ้นปี → งบทดลอง/GL รายปีถูกต้อง แต่การกรองรายเดือนภายในปีไม่แม่นยำ
