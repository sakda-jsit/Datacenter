# 19 Validation Rules (Requirement v11 — รวมศูนย์)

> รวม VR จาก workbook v11 (09-validation-rules) เพื่อใช้เป็น checklist ก่อน final report

## Trial Balance & Mapping
- VR-001: Account code ใน TB ต้องมี mapping ใน master (T) ก่อนออกงบ
- VR-002: TB ต้องสมดุล debit = credit ทั้งก่อนและหลัง adjustment; ไม่สมดุล → ระบุ account ที่เป็นเหตุ
- VR-003: adjustment ต้องมี debit/credit + เหตุผล + เอกสารอ้างอิง + source type
- VAL-NEW-002: account ที่ไม่มี mapping group code → ห้ามออกงบจนกว่าจะครบ
- VAL-NEW-003: group code ต้องมีใน DBD master (`npae_com-oth_...xls`)

## รอบบัญชี / Subsequent
- VR-004 / VR-015: วันที่ต้องอยู่ในรอบบัญชีที่เลือก; ข้อมูลนอกช่วง (ปีถัดไป) ต้อง tag เป็น reference
- VAL-NEW-006: รายการปีถัดไปห้ามนำมารวมยอดปีปิดงบอัตโนมัติ

## VAT / Tax
- VR-005: ภาษีขาย/ซื้อ สัมพันธ์กับฐานภาษี × อัตรา; ต่างจากมาตรฐานต้องระบุเหตุผล
- VR-006: ภ.พ.30 ภาษีสุทธิ = ภาษีขาย − ภาษีซื้อ
- VR-007: balance ภาษีต่อเนื่องรายเดือน (PP.30/PND.3/PND.53)
- VR-TAX-002: PP.30 ต้องตรง Input/Output VAT รายเดือน
- VR-TAX-003: PDF ภ.ง.ด.3/53 ต้องตรง Express ก่อนบันทึก (แบบ/เดือน/ราย/เงินได้/ภาษี/เงินเพิ่ม/ชำระ)
- VR-TAX-004: ยื่นเพิ่มเติมหลายครั้งห้าม overwrite ของเดิม
- VR-TAX-005: เก็บ PDF ต้นฉบับ + metadata
- VR-008: WHT ต้องมีข้อมูลผู้ถูกหักครบ (ชื่อ/เลขผู้เสียภาษี/สาขา/ที่อยู่/วันที่จ่าย/ประเภทเงินได้/อัตรา/ฐาน/ภาษี)

## Fixed Asset / Prepaid
- VR-009 / VR-NEW-010: FA ครบก่อนคำนวณค่าเสื่อม; แยกบัญชี/ภาษีไม่ปนกัน; รายงานระบุชุด
- VR-NEW-011: disposal ต้องมีวันที่/ประเภท/สินทรัพย์/ราคาทุน+ค่าเสื่อมสะสมก่อน/เอกสาร
- VR-010 / VR-NEW-012: Prepaid วันเริ่ม ≤ สิ้นสุด; ตัดจ่ายรวม ≤ มูลค่าตั้งต้น; คงเหลือไม่ติดลบ; คงเหลือ = ตั้งต้น − สะสม

## AR/AP / Stock
- VR-011: AR/AP net + WHT + bank charge + adj สัมพันธ์ invoice/bill (ไม่ตัดเกิน)
- VR-ARAP-RECON-001~004: status ครบ; matched มีหลักฐาน Express+bank ครบ; partial/unmatched แสดงส่วนต่าง; WHT/bank charge จาก Express
- VAL-STOCK-001~005: stock ครบก่อนใช้; warehouse ถูกต้อง; FIFO cost มี; FG≠TB → unmatched/pending; ห้าม final ถ้ายังไม่ reconcile

## Financial Statement / NOTE2
- VR-FS-001: layout BAL1/BAL2/PL/CAP/NOTE2 ตรง Excel เดิม (Page Break Preview)
- VR-FS-002: ตัวเลขใน NOTE2 trace กลับ TB ปีปัจจุบัน/ปีก่อนได้ ห้าม key เองไม่มี source
- VR-FS-004: header หลายหน้าถูกต้อง ไม่พึ่ง OLE_LINK เป็น external dependency

## Control / Report
- VR-CTRL-001~005: ทุกแก้ไขมี audit trail (old/new); รายการที่ต้องมีเอกสารต้องแนบครบก่อน final; audit export ครอบคลุมครบ; attachment มี metadata ครบ
- VR-016/017: final report มีสถานะ/version; แสดง validation สำคัญก่อนออก final (TB balance, missing mapping, VAT/WHT, adjustment ค้าง, recon ค้าง)

## Migration
- parallel run ปี 2025 **variance = 0**; แยกประเภทผลต่างถ้ามี (mapping/data source/formula/rounding/user adjustment)
- final ไม่ควร lock จนกว่า parallel run ผ่านการตรวจครบ
