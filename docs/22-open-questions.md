# 22 Open Questions (คงเหลือหลังรอบ v11 + ทบทวน 2026-06-04)

> open questions ที่ User ตอบแล้ว ดู docs/12 (หัวข้อ "คำตอบ Open Questions ที่ใช้เป็นฐานพัฒนา")
> ไฟล์นี้เก็บเฉพาะที่ **ยังค้าง** ต้องยืนยันก่อน/ระหว่างพัฒนา

## ค้างจากรอบทบทวน
1. ~~**Report review/lock**~~ → **ตอบแล้ว 2026-06-04**: เก็บ version final เริ่มจาก v0; ยื่นงบแล้ว lock ห้ามแก้;
   ยื่นเพิ่มเติม = เปิด version ใหม่ (v1, v2...) version เดิม freeze ถาวร; ปลดล็อก/เปิด version ใหม่ได้ทุกคนที่มีสิทธิ์ในบริษัท + audit. ดู docs/18
2. **ผู้ sign-off parallel run**: ฝ่ายบัญชี / IT / ผู้สอบบัญชี / ผู้บริหาร? → กระทบ docs/21

## ค้างต่อจาก workbook v11 (ยังไม่ได้ถาม User)
### Calculation / Asset
3. ค่าเสื่อมบัญชี vs ภาษี — ระบบสร้าง journal adjustment อัตโนมัติ หรือแสดงเป็น working paper ให้บัญชีบันทึกเอง?
   (เทียบกับ Stock ที่ยืนยันแล้วว่าให้บันทึกเอง — ควร confirm ให้สอดคล้องกัน)

### Stock
4. รายงาน stock แสดงระดับ cost layer รายตัว หรือเฉพาะยอดสรุปต่อ item/warehouse?

### Tax
5. PP.30 / PND ที่บล็อก DBF — โครงสร้างตาราง Input/Output VAT และ WHT จริงของ Express เป็นอย่างไร?
   (ตัวบล็อกหลักของ VAT/PP30/AR/AP)

### Report Package
6. รูปแบบไฟล์/field เฉพาะของแต่ละหน่วยงาน (ผู้สอบบัญชี/DBD/สรรพากร/ประกันสังคม) — ต้องยืนยันก่อนทำ export เฉพาะทาง
7. ต้องมีรายงานสรุป mapping "สูตร Excel เดิม → business rule ใหม่" ส่งผู้สอบบัญชีหรือไม่?

### Adjustment
8. Leasing/Loan working paper — field/สูตรที่ต้องคำนวณในหน้าจัดการ (จากไฟล์ `2025_JSPC_LEASING(1).xlsx`,
   `2025_CRUVE_LOAN.xlsx`) ต้องขอตัวอย่างไฟล์จริงเพื่อถอด schedule logic

## หมายเหตุ
- ข้อ 5 เป็น blocker หลักของโมดูล VAT/PP30/AR/AP (docs/16, docs/17) — ควรเร่งขอสเปก DBF
- ข้อ 8 จำเป็นก่อนเริ่มพัฒนาโมดูล Leasing/Loan (docs/13)
