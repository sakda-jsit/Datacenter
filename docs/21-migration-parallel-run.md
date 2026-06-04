# 21 Migration & Parallel Run (Requirement v11)

> ที่มา: คำตอบ User A-035~037 + #6 (2026-06-04)

## Migration Baseline
- เริ่ม migrate ตั้งแต่ปีบัญชี **2025/2568** เป็นต้นไป
- **ไม่ migrate** hidden sheet ย้อนหลังปี 2016–2024
- อ้างอิงข้อมูลปีก่อนในรายงานเฉพาะที่จำเป็นและอยู่ในพื้นที่ใช้งานจริง (Page Break Preview)

## Formula Migration
- สูตร Excel ที่ใช้งานจริงทั้งหมดต้อง migrate เป็น business rule — ไม่เหลือขั้นตอนคำนวณหลักใน Excel
- ครอบคลุม: TB mapping/adjustment, FS generation, NOTE2 data linkage, TAX, VAT/PP30, PND.3/53,
  asset depreciation (บัญชี+ภาษี), prepaid amortization, AR/AP recon, stock/FIFO + FG↔TB, audit/evidence
- จัดทำ **formula inventory** แยกตาม module; สูตรที่ยังไม่เข้าใจ = open issue (ห้ามเดา logic บัญชี)
- business rule ต้อง trace ได้ว่ามาจาก sheet/สูตรใดใน workbook เดิม

## Parallel Run (คำตอบ #6)
- ทำ parallel run **1 งวด = ปี 2025/2568** เทียบ Excel เดิม
- **เกณฑ์ยอมรับ: ตรงเป๊ะทุกยอด (ผลต่าง = 0)** — ไม่มี rounding tolerance
- เทียบอย่างน้อย: adjusted TB, งบการเงิน (BAL/PL/CAP/NOTE2), TAX, VAT/PP30, PND, FA, Prepaid, AR/AP recon, Stock/FIFO
- ถ้าพบผลต่าง → แยกประเภท (mapping / data source / formula / rounding / user adjustment) และแก้ rule
- ต้อง sign-off ก่อน go-live (ใครเป็นผู้ sign-off = OPEN QUESTION, ดู docs/22)

## Recommended Sequence
1. formula inventory + ระบุพื้นที่ใช้งานจริงของ workbook ปี 2025
2. แยกสูตรตาม module → ยืนยัน business rule กับฝ่ายบัญชี
3. นำ Express snapshot + ไฟล์หลักฐานปี 2025 เข้าระบบ
4. generate รายงาน → เทียบ Excel เดิม → บันทึก variance → แก้ rule
5. sign-off → go-live

## Migration Test Cases (ต่อจาก docs/11 เดิม + เพิ่ม req v11)
- import TB → debit/credit รวมตรง Excel
- mapping account → group code ตรง TB25
- adjusted final debit/credit ตรง TB25
- BAL1/BAL2/PL/CAP ตรง Excel ทีละบรรทัด
- PP.30 / PND ตรงรายเดือน; FA + SUM ตรง; Prepaid ตรงรายเดือน
- AR/AP จับคู่ตรง; Stock/FG ↔ TB ตรง
- NOTE2 ตัวเลขตรง + layout ตรง Page Break Preview
