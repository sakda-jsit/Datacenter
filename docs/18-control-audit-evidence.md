# 18 Control / Audit / Evidence / Report Package (Requirement v11)

> ที่มา: คำตอบ User A-031~034 + #7/#8/#9/#13 (2026-06-04)

## สิทธิ์แก้ไข/ลบ
- ผู้ใช้ที่มีสิทธิ์ในบริษัทนั้น **แก้ไขและลบ** ข้อมูลสำคัญได้ทุกคน (master mapping, adjustment, tax, asset, prepaid)
- **ไม่มี approval workflow** — ชดเชยด้วย field-level audit trail (คำตอบ #7: ทุกคนลบได้ + audit trail)
- ยังคงผูก multi-company isolation (CompanyUserAccess) เดิม — เปิดสิทธิ์ภายในบริษัทที่เข้าถึงได้เท่านั้น

## Field-Level Audit Trail
ทุก create/update/delete ของ field ที่ผู้ใช้แก้ได้ ต้องบันทึก:
- module/entity, record ref, field name, old value, new value
- action type (create/update/delete), user, datetime, reason/note, attachment ref

แนวทาง implement: ขยาย `AuditLog` → `FieldAuditLog` (เช่น EF Core SaveChanges interceptor บันทึก diff)

## Attachment / Evidence Management ✅ เสร็จ (2026-06-07)
- รายการสำคัญต้องแนบเอกสารได้: bank statement, ใบกำกับภาษี, ใบหัก ณ ที่จ่าย, PDF สรรพากร, เอกสาร asset/prepaid, ไฟล์ Express/import
- metadata: ประเภท, ชื่อไฟล์, module/record, ปีบัญชี, ผู้อัปโหลด, วันที่, สถานะตรวจสอบ, checksum
- **Evidence completeness check**: ก่อน mark พร้อมปิดงบ/สร้าง final report ต้องตรวจว่าแนบเอกสารครบ ไม่ครบ → warning

> **Implement:** `Attachment` entity (blob ใน DB + SHA-256, polymorphic `ModuleName`+`RecordId`+`FiscalYear`) + enum `AttachmentCategory` (12 หมวด) / `AttachmentVerificationStatus` (Pending/Verified/Rejected). CQRS Upload(multipart)/UpdateMetadata/SetVerification/Delete + GetAttachments(filter)/GetAttachmentContent(audit ทุกการดาวน์โหลด)/GetEvidenceCompleteness. `EvidenceChecklist` (static: 4 หมวดบังคับ = งบการเงิน/หนังสือยืนยันยอดธนาคาร/bank statement/แบบสรรพากร) → completeness query นับต่อหมวด/ปี (เอกสาร FiscalYear=null ใช้ได้ทุกปี). `AttachmentsController` /api/v1/attachments (GET, /completeness, /{id}/download, POST multipart, PUT /{id}, PUT /{id}/verification, DELETE). Frontend เมนู "คลังเอกสาร / หลักฐาน" `/evidence` 2 แท็บ (เอกสารแนบ+filter+upload/verify/delete/download / ความครบถ้วน checklist). ทุก action ลง audit trail. เก็บถาวร ≥ 10 ปี. migration AddAttachments.

## Audit Log Export (A-034) ✅ เสร็จ (2026-06-07)
- export ให้ผู้สอบบัญชี: รูปแบบ **Excel / PDF / CSV** (คำตอบ #8) — ดึง **ทั้งชุดตามตัวกรอง** (ไม่ใช่แค่หน้าปัจจุบัน) ผ่าน `GET /audit-log/export` (cap 50,000 + เตือนถ้าเกิน)
- filter: บริษัท, module (EntityName), ประเภทรายการ (Action), ผู้แก้/รหัส (search), ช่วงวันที่ — ตัวเลือก Action/EntityName จาก `GET /audit-log/filter-options`
- แสดงจำนวนรายการรวมใน subtitle ของไฟล์เพื่อยืนยันความครบ
- **ยังไม่ครอบ:** filter ตาม ปีบัญชี/report package โดยตรง (ใช้ช่วงวันที่แทนได้)

## Report Package & Final Version Control (ยืนยัน 2026-06-04)
- จัดชุดรายงานตามผู้รับ: ผู้สอบบัญชี / กรมพัฒนาธุรกิจการค้า / กรมสรรพากร / ประกันสังคม / อื่น ๆ
- มีสถานะ Draft / Review / Final

### Final Versioning & Lock-after-Submission
- **เก็บ version ของ final โดยเริ่มจาก version 0** (v0 = ฉบับยื่นครั้งแรก)
- **เมื่อยื่นงบแล้ว → ระบบ lock ห้ามแก้ไขข้อมูลใด ๆ ที่ประกอบ version นั้น** (งบ + working paper/แหล่งตัวเลขที่ผูกกับ version)
- version ที่ยื่นไปแล้ว **freeze ถาวร** — เก็บเป็นหลักฐานไม่เปลี่ยนแปลง
- การแก้ไข/ยื่นเพิ่มเติม (รองรับไม่จำกัดครั้ง) ต้อง **เปิด version ใหม่** (v1, v2, ...) ไม่ทับ version เดิม
- **ผู้เปิด version ใหม่/ปลดล็อกเพื่อแก้ = ทุกคนที่มีสิทธิ์ในบริษัท** (ไม่จำกัด role) แต่ต้องบันทึก audit trail ทุกครั้ง (สอดคล้อง A-031)
- ทุกการเปลี่ยน version และการ lock/unlock ต้องลง audit trail (ใคร/เมื่อไร/version เดิม→ใหม่/เหตุผล)

> เชื่อมกับ Tax module (docs/16): การยื่นเพิ่มเติมของแบบภาษีก็ผูกกับ version ใหม่เช่นกัน — แต่ละ submission แยกหลักฐาน
> เชื่อมกับ Closing Period เดิม: ระบบมี reopen/lock งวด (Admin) อยู่แล้ว — final-version lock เป็นชั้นเพิ่มที่ระดับ report package/การยื่น

## Retention (คำตอบ #13)
- เอกสารแนบ + audit log + import snapshot เก็บอย่างน้อย **10 ปี** (กฎหมายบัญชี/สรรพากร)
