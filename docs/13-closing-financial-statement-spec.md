# 13 Closing & Financial Statement Spec (Requirement v11)

> ที่มา: workbook `2025_JSPC_FIN.xlsx` (sheets TB25/TB24, T, BAL1/BAL2/PL/CAP, NOTE2, CASH COUNT, INTEREST INCOME, 22120)
> คำตอบ User ยืนยัน 2026-06-04 ฝังไว้ในแต่ละหัวข้อ

## 1. Adjusted Trial Balance (TB25/TB24)
โครงสร้างคอลัมน์ที่ต้องรองรับต่อบัญชี:
- ยอดยกมา (B/F debit/credit)
- เคลื่อนไหวระหว่างงวด (in-period debit/credit)
- ยอดคงเหลือก่อนปรับปรุง (balance debit/credit)
- รายการปรับปรุง (Adj debit/credit)
- ยอดหลังปรับปรุง (final debit/credit)

สูตรอ้างอิง (Excel เดิม):
```
Debit final  = max(BalDr + AdjDr − BalCr − AdjCr, 0)
Credit final = abs(min(BalDr + AdjDr − BalCr − AdjCr, 0))
group code   = VLOOKUP(account, T, group)
```
- ตรวจ debit = credit ทั้งก่อนและหลัง adjustment (VR)
- ไม่เก็บ version ทุกครั้ง → คำนวณซ้ำจาก Express snapshot + mapping + adjustment ปัจจุบัน

## 2. Adjustment Entry & Leasing/Loan Working Paper
- adjustment ต้องมี: debit/credit สมดุล, SourceType (Leasing/Loan/Manual/Tax/Other), reference, เหตุผล, attachment
- **Leasing/Loan (คำตอบ #11): มีหน้าจัดการในระบบ** — ผู้ใช้บันทึก/คำนวณ schedule (สัญญา, เงินต้น, อัตรา, งวด) ในระบบ
  เมื่อเสร็จ ระบบ generate adjustment entry เข้า TB ปีปัจจุบันอัตโนมัติ พร้อมเก็บไฟล์อ้างอิง
- ไฟล์ต้นทางตัวอย่าง: `2025_JSPC_LEASING(1).xlsx`, `2025_CRUVE_LOAN.xlsx`

## 3. Master Mapping & DBD Group-Code Taxonomy (sheet T)
- mapping Account Code → group code (A1/A2/L1...) ดูแลโดยเจ้าหน้าที่บัญชี ไม่มี approval (มี audit trail)
- group code = master taxonomy มาตรฐานกรมพัฒนาธุรกิจการค้า อ้างอิง `npae_com-oth_2026-05-04_0205567059050.xls`
- validate: account ที่ไม่มี mapping ห้ามออกงบ; group code ต้องมีใน master

## 4. งบการเงินหลัก (BAL1/BAL2/PL/CAP)
- ดึงยอดจาก TB ปีปัจจุบัน + ปีก่อนด้วย SUMIF ตาม group code
  - สินทรัพย์: `SUMIF(Dr) − SUMIF(Cr)`
  - หนี้สิน/ทุน/รายได้: กลับ sign `−SUMIF(Dr) + SUMIF(Cr)`
- CAP (งบเปลี่ยนแปลงส่วนผู้ถือหุ้น): ทุนเรือนหุ้น + กำไรสะสม + ผลประกอบการปีปัจจุบัน (ดึงจาก PL)
- **รูปแบบต้องตรง Excel เดิม 100%** ตาม Page Break Preview (heading, subtotal, total, note ref, ตำแหน่งตัวเลข)

## 5. NOTE2 (หมายเหตุประกอบงบ)
แยก 2 ส่วนชัดเจน:
1. **Template text/form** — User แก้ได้เมื่อมาตรฐานบัญชีเปลี่ยน (มี EffectiveYear)
2. **Data binding** — ตัวเลขดึงจาก TB ปีปัจจุบัน/ปีก่อน + schedule (SUM/FA) อัตโนมัติ ห้ามแก้ตรง
- `OLE_LINK` = report header ให้เหมือนหน้าแรกเท่านั้น → ทำเป็น page header ไม่ใช่ data source
- migrate เฉพาะพื้นที่ Page Break Preview; hidden sheet 2016–2024 อยู่นอก scope

## 6. Cash Count (CASH COUNT)
- บันทึกตรวจนับเงินสด: ชนิดธนบัตร × จำนวนฉบับ = มูลค่า (`ROUND(qty×value,2)`)
- แนบ bank evidence/statement อ้างอิง

## 7. Interest Income (INTEREST INCOME / 22120 เงินกู้กรรมการ)
```
ดอกเบี้ย = เงินต้นคงเหลือ × อัตรา × จำนวนวัน / ฐานวันต่อปี (365)
+ ภาษีธุรกิจเฉพาะ + รายได้ส่วนท้องถิ่น (อัตราตาม sheet)
```
- ยอดคงเหลือเงินต้นสะสม: `Balance = prev + เพิ่ม − ลด`

## Export
- งบการเงิน export Excel + PDF (คำตอบ #8); report package draft/review/final/lock (ดู docs/18)
