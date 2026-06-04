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

---

# Business Rules เพิ่มเติม (Requirement v11)

> สกัดจาก workbook ปิดงบจริง JSP CONNEX + คำตอบ User ยืนยัน 2026-06-04
> รายละเอียดสูตร/calculation อยู่ในไฟล์ spec ใหม่ docs/13–21; ส่วนนี้สรุปกฎระดับ business

## Trial Balance & Adjustment
- งบทดลองต้องแยกยอดก่อนปรับปรุงและหลังปรับปรุง (B/F, in-period, balance, adj, final)
- adjustment ทุกรายการต้องมี debit/credit สมดุล + source type (Leasing/Loan/Manual/Tax/Other) + reference + เหตุผล
- adjustment จาก Leasing/Loan: ผู้ใช้คำนวณใน **หน้าจัดการในระบบ** เสร็จแล้วระบบส่งผลเข้า TB ปีปัจจุบันอัตโนมัติ
- ไม่เก็บ version ของ adjusted TB ทุกครั้ง — คำนวณซ้ำจาก Express snapshot + mapping + adjustment ปัจจุบัน (audit trail อธิบายที่มา)

## Financial Statement
- BAL1/BAL2/PL/CAP/NOTE2 ต้องตรงรูปแบบ Excel เดิม 100% (Page Break Preview)
- ตัวเลขทั้งหมดดึงจาก TB ปีปัจจุบัน + ปีก่อน (ห้าม key ตรง ยกเว้น manual adjustment ที่มี reference)
- NOTE2 แยก template text/form (User แก้ได้เมื่อมาตรฐานบัญชีเปลี่ยน) ออกจาก data binding; `OLE_LINK` = report header เท่านั้น
- รหัสกลุ่มงบ (A1/A2/L1...) ต้อง validate กับ master taxonomy กรมพัฒนาธุรกิจการค้า (`npae_com-oth_...xls`)

## Fixed Asset
- `FA` = ทะเบียนสินทรัพย์หลัก; ค่าเสื่อม 2 ชุดแยกกัน (บัญชี + ภาษี) ห้ามปนกัน
- อัตรา/อายุค่าเสื่อมมาจาก **master มาตรฐานต่อประเภทสินทรัพย์** (override รายตัวได้)
- รองรับซื้อเพิ่ม/จำหน่าย/ขาย/ตัดจำหน่าย → กระทบราคาทุน/ค่าเสื่อมสะสม/มูลค่าสุทธิ/SUM/NOTE2
- **ขายสินทรัพย์: ระบบคำนวณกำไร/ขาดทุนอัตโนมัติ** = ราคาขาย − มูลค่าสุทธิ ณ วันขาย

## Prepaid
- ทุกรายการใช้ pattern การตัดจ่ายเดียวเป็น rule กลาง; ยอดตัดจ่ายรวม ≤ มูลค่าตั้งต้น; คงเหลือไม่ติดลบ

## Stock / Inventory
- ต้นทุน FIFO อ้างอิงจาก Express เท่านั้น (ไม่คำนวณต้นทุนใหม่เอง); รองรับหลายคลัง
- `FG` reconcile กับ TB ปีปัจจุบัน — **กรณีต่าง: ระบบแสดงผลต่าง ให้บัญชีบันทึก adjustment เอง (ไม่ auto)**
- ห้ามใช้ยอด stock ที่ยังไม่ reconcile เป็น final โดยไม่เตือน

## Tax
- `TAX`: ระบบคำนวณภาษีเงินได้นิติบุคคลทั้งหมดจาก TB + add-back/deduct (ต่อยอดจาก ภ.ง.ด.50)
- `PP.30`: ดึง Input/Output VAT อัตโนมัติ; ภาษีสุทธิ = ภาษีขาย − ภาษีซื้อ; balance carry-forward รายเดือน
- `PND.3/53`: upload PDF สรรพากร (layout คงที่ → template parser) → reconcile กับ Express → เก็บเฉพาะเมื่อตรง
- รองรับเบี้ยปรับ/เงินเพิ่ม + ยื่นเพิ่มเติมไม่จำกัดครั้ง/เดือน (แยกแต่ละ submission ห้าม overwrite)

## AR/AP Reconciliation
- จับคู่ Express (receipt/payment + WHT + bank charge) กับ bank statement
- status: `matched` / `partial` / `unmatched`; partial/unmatched ต้องแสดงส่วนต่างให้บัญชีตรวจก่อนปิดงบ
- WHT + bank charge ดึงจาก Express เป็นหลัก; แก้ไขเองได้แต่ต้องมีเหตุผล + audit trail

## Subsequent Payment
- ใช้ข้อมูลปีถัดไป (`GL1`/`JV1`) ตรวจรายการค้างจ่ายปีปิดงบ: paid/partial/unpaid/unmatched
- ข้อมูลปีถัดไปเป็น reference เท่านั้น — ห้ามนำมารวมยอดปีปิดงบ

## Control / Security / Evidence
- **ทุกคนที่มีสิทธิ์ในบริษัทแก้ไข/ลบข้อมูลสำคัญได้** (ไม่มี approval) แต่ทุกการ create/update/delete ต้องมี field-level audit trail
- audit trail เก็บ: module, record ref, field, old value, new value, user, datetime, action type, reason, attachment ref
- รายการสำคัญต้องแนบเอกสาร (statement/ใบกำกับภาษี/ใบหัก ณ ที่จ่าย/PDF สรรพากร/ไฟล์ Express) + ตรวจ completeness ก่อน final
- audit log ต้อง export ให้ผู้สอบบัญชี (Excel/PDF/CSV) filter ตามบริษัท/ปี/module/ผู้แก้/ช่วงวัน
- เอกสารแนบ + audit log เก็บอย่างน้อย **10 ปี**
- **Final report versioning**: เก็บ version ของ final เริ่มจาก v0; **เมื่อยื่นงบแล้ว → lock ห้ามแก้ข้อมูลที่ประกอบ version นั้น** (freeze ถาวร); ยื่นเพิ่มเติม = เปิด version ใหม่ (v1, v2...); ปลดล็อก/เปิด version ใหม่ได้ทุกคนที่มีสิทธิ์ในบริษัท + audit trail (ดู docs/18)

## Express Data Source & Migration
- "เชื่อม DB Express" = อ่านไฟล์ DBF โดยตรง (pipeline ปัจจุบัน ISINFO/GLACC/GLBAL/ISPRD)
- ดึงข้อมูลแบบ **snapshot ตามรอบปิดงบ + เก็บถาวร** — ยอดปิดงบไม่เปลี่ยนเมื่อ Express ถูกแก้ภายหลัง
- Migration baseline = ปี 2025/2568; ไม่ migrate hidden sheet 2016–2024; สูตร Excel ที่ใช้จริง migrate เป็น business rule ทั้งหมด
- **Parallel run ปี 2025 ต้องตรงเป๊ะทุกยอด (ผลต่าง = 0)** ก่อน go-live

# Validation Rules (สรุป — รายละเอียดเต็มใน docs/19)
- VR: Account code ใน TB ต้องมี mapping; TB debit=credit (ก่อน/หลัง adj); adjustment ต้องมีเหตุผล+เอกสาร
- VR: วันที่อยู่ในรอบบัญชีที่เลือก; ข้อมูลปีถัดไปต้อง tag เป็น reference
- VR: ภ.พ.30 = ภาษีขาย−ภาษีซื้อ และตรงกับ Input/Output VAT; balance ภาษีต่อเนื่องรายเดือน
- VR: PDF ภ.ง.ด.3/53 ต้องตรง Express ก่อนบันทึก; เก็บไฟล์ต้นฉบับ
- VR: FA แยกบัญชี/ภาษี; disposal ต้องมีข้อมูลอ้างอิง; Prepaid ตัดจ่ายรวม ≤ มูลค่าตั้งต้น
- VR: AR/AP recon status ครบ; matched ต้องมีหลักฐาน Express + bank ครบ
- VR: Stock/FG ต่างจาก TB → unmatched/pending; ห้าม final โดยไม่เตือน
- VR: ทุกการแก้ไขต้องมี audit trail (old/new); รายการที่ต้องมีเอกสารต้องแนบครบก่อน final
- VR: รายงาน final ต้องมีสถานะ/version; migration parallel run variance = 0
