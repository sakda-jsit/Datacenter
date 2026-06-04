# 20 Express Integration & Snapshot (Requirement v11)

> ที่มา: คำตอบ User A-002/A-005/A-006/A-008 + #1/#10 (2026-06-04)

## รูปแบบการเชื่อมต่อ (คำตอบ #1)
- "เชื่อมฐานข้อมูล Express โดยตรง" = **อ่านไฟล์ DBF โดยตรง** (pipeline ปัจจุบัน) — ไม่ใช่ Excel export, ไม่ต้องเปลี่ยนสถาปัตยกรรม import
- ปัจจุบันอ่าน: ISINFO/GLACC/GLBAL/ISPRD (อ่านด้วย FileShare.ReadWrite รองรับ Express เปิดไฟล์ค้าง)
- dataset ที่ต้องขยายให้ครบตาม workbook: TB, GL, JV, RE, PV, STOCK, INPUT/OUTPUT VAT, account-specific (11521/11550/11572/INCOME/COST/53220)

## Schema คงที่ แต่ต้อง validate (A-005/BR-013)
- รูปแบบ Express คงที่ทุกปี → กำหนด template/schema กลางต่อ dataset และใช้ซ้ำข้ามปี
- ยัง validate field/column สำคัญทุกครั้งก่อนประมวลผล (กัน export เปลี่ยนรูปแบบจาก config อื่น)
- Account code/name คงที่ (A-006) → mapping ใช้ซ้ำข้ามปี แต่แจ้งเตือน account ใหม่ที่ยังไม่ map

## Snapshot ตามรอบปิดงบ (คำตอบ #10) — สำคัญ
- ดึงข้อมูลแบบ **snapshot ตามรอบปิดงบ และเก็บถาวร** ไม่ใช่ real-time
- เหตุผล: ยอดที่ปิดงบแล้วต้องไม่เปลี่ยนเมื่อ Express ถูกแก้ภายหลัง
- เก็บ `ImportSnapshot`: ปีบัญชี, source type, วันที่ดึง, source file path, checksum, row count, สถานะ
- เก็บไฟล์ต้นฉบับ (A-008/BR-016) ต่อ import batch — retention ≥ 10 ปี

## Import Evidence Log (RPT-020)
ประเภทข้อมูล, ชื่อไฟล์/batch, ปีบัญชี/รอบ, ผู้นำเข้า/วันที่, สถานะ validation, จำนวนรายการ, link หลักฐาน

## Adjustment ที่ไม่ได้มาจาก Express
- Leasing/Loan adjustment มาจากหน้าจัดการในระบบ (docs/13) — แยก module ออกจาก Express import, trace แหล่งทาง

## ข้อจำกัดที่ทราบ (ต่อจาก docs/12)
- Express export เป็นยอดรวมรายปี → รายปีถูกต้อง รายเดือนภายในปีไม่แม่นยำ
- posting รองรับทีละ 1 ปีงบ/บริษัท → หลายปีต่อเนื่อง = Phase 2 (redesign)
