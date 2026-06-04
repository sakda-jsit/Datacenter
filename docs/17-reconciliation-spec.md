# 17 AR/AP Reconciliation & Subsequent Payment Spec (Requirement v11)

> ที่มา: workbook sheets `AR-RECEIPT`, `RE`, `AP-PAYMENT`, `PV`, `GL1`, `JV1` + คำตอบ User

## AR/AP Reconciliation + Bank Statement
จับคู่ 2 แหล่ง: Express (receipt/payment/WHT/bank charge) + Bank statement (ยืนยันเงินจริง)

### Flow
1. ดึงรับชำระจาก Express → `AR-RECEIPT`; จ่ายชำระ → `AP-PAYMENT`
2. ดึง WHT + bank charge จาก Express (A-026: ไม่ใช่ user input หลัก)
3. นำ bank statement มาจับคู่รายการเงินจริง
4. ระบุสถานะ reconcile

### สถานะ (A-027)
- `matched` = ยอด+รายการตรงครบ (ต้องมีหลักฐาน Express + bank ครบ)
- `partial` = ตรงบางส่วน/มีส่วนต่าง
- `unmatched` = ยังจับคู่ไม่ได้
- partial/unmatched → แสดงส่วนต่าง/เหตุผล ให้ฝ่ายบัญชีตรวจก่อนปิดงบ

### Validation
- net receipt/payment + WHT + bank charge + adjustment ต้องสัมพันธ์กับ invoice/bill amount (ไม่ตัดเกิน)
- WHT/bank charge แก้เองได้แต่ต้องมีเหตุผล + audit trail

### รายงาน
- RPT-015 AR: ลูกค้า, Invoice, Receipt, Amount, WHT, Bank charge, Net, ส่วนต่าง, bank ref, สถานะ
- RPT-016 AP: Vendor, Voucher, Payment, Amount, Date, WHT, Bank charge, ส่วนต่าง, bank ref, สถานะ

> **บล็อก DBF**: รอโครงสร้างตารางลูกหนี้/เจ้าหนี้/รับ-จ่ายชำระจริงของ Express + รูปแบบ bank statement

## Subsequent Payment Check (GL1/JV1)
ตรวจรายการค้างจ่ายปีปิดงบว่าจ่ายชำระแล้วหรือยังในปีถัดไป
```
Accrued/Payable ณ สิ้นปี → ค้นหา payment/journal ในปีถัดไป (GL1/JV1/PV)
→ classify: paid / partial paid / unpaid / unmatched
```
- ข้อมูลปีถัดไปเป็น **reference เท่านั้น** — ห้ามนำมารวมยอดปีปิดงบ (tag เป็น subsequent evidence)

### รายงาน (RPT-019)
ปีที่ตั้งค้างจ่าย, account/vendor/reference, ยอดค้าง, วันที่ตั้ง, ข้อมูลจ่ายปีถัดไป (วันที่/เลขที่/ยอด), สถานะ, แหล่งตรวจ
