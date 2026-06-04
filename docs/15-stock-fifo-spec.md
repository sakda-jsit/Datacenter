# 15 Stock / Inventory / FIFO Spec (Requirement v11)

> ที่มา: workbook sheets `STOCK2025`, `STOCK2024`, `FG2025` + คำตอบ User 2026-06-04

## หลักการ
- Stock เป็นทั้ง **data source (รายงาน)** และ **calculation source (ต้นทุน)** — ไม่ใช่รายงานประกอบเท่านั้น
- ต้นทุนแบบ **FIFO อ้างอิงจาก Express เท่านั้น** (ไม่คำนวณต้นทุนใหม่เองโดยไม่มีหลักฐาน)
- รองรับ **หลายคลัง (warehouse)**

## ข้อมูลที่ต้องดึง/รองรับ (จาก Express snapshot)
- ปีบัญชี/รอบบัญชี, Item code/name, Warehouse, Quantity balance
- Unit cost / FIFO cost layer, Amount balance
- Account mapping สำหรับเทียบ TB

## การดึงข้อมูล (คำตอบ #10)
- **snapshot ตามรอบปิดงบ + เก็บถาวร** (ไม่ real-time) — ยอดปิดงบไม่เปลี่ยนเมื่อ Express ถูกแก้ภายหลัง
- เก็บ metadata: วันที่ดึง, รอบบัญชี, ผู้ดึง, source, checksum, batch id

## FG ↔ TB Reconciliation (FG2025)
แสดง: ยอดจาก Express/FG, ยอดบัญชีสินค้าคงเหลือจาก TB, ผลต่าง, สถานะ
- สถานะ: Matched / Unmatched / PendingReview
- **กรณีต่าง (คำตอบ #5): ระบบแสดงผลต่าง ให้ฝ่ายบัญชีบันทึก adjustment เอง — ไม่สร้าง adjustment อัตโนมัติ**
- ห้ามใช้ยอด stock ที่ยังไม่ reconcile เป็น final โดยไม่เตือน (VR)

## Validation
- ตรวจความครบถ้วน stock ของรอบที่เลือกก่อนใช้คำนวณ
- ทุกรายการต้องมี warehouse ที่ถูกต้อง (อยู่ใน master)
- รายการที่ใช้ FIFO ต้องมี cost/amount; ผิดปกติ → exception

## รายงาน
- RPT-STOCK-001: Item, Warehouse, Quantity, FIFO/Unit cost, Amount, รวมตาม item/warehouse/ทั้งหมด
- RPT-STOCK-002: FG เทียบ TB (Express vs TB + ผลต่าง + สถานะ + หมายเหตุ)
- ต้อง trace ยอดต้นทุนกลับไป stock/warehouse/cost source ได้
