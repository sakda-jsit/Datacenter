# Implementation Status

สถานะการพัฒนา Phase 1 (อัปเดต: 2026-06-02)

## โมดูล (Phase 1)

| # | โมดูล | สถานะ | หมายเหตุ |
|---|---|---|---|
| 1 | Client Management | ✅ เสร็จ | full-stack |
| 2 | Import Data (Express DBF) | ✅ เสร็จ | ISINFO/GLACC/GLBAL/ISPRD + auto-post |
| 3 | VAT Management | ⛔ รอสเปก DBF | ต้องการโครงสร้างตารางภาษีซื้อ/ขายของ Express |
| 4 | AR Management | ⛔ รอสเปก DBF | ต้องการตารางลูกหนี้/ใบแจ้งหนี้ |
| 5 | AP Management | ⛔ รอสเปก DBF | ต้องการตารางเจ้าหนี้/ใบรับวางบิล |
| 6 | Payroll | ⛔ รอสเปก DBF | ต้องการข้อมูลเงินเดือน + สูตรภาษี/ประกันสังคม |
| 7 | Bank Reconciliation | ⛔ รอสเปก | ต้องการ statement ธนาคาร |
| 8 | Trial Balance | ✅ เสร็จ | อ่านจาก Account + JournalEntry (ต้อง post ก่อน) |
| 9 | General Ledger | ✅ เสร็จ | running balance ต่อบัญชี |
| 10 | Financial Statement | ✅ เสร็จ | งบฐานะการเงิน + กำไรขาดทุน (ต้องตั้ง mapping บัญชี→บรรทัดงบ) |
| 11 | Tax Report (PP30/PND) | 🟡 บางส่วน | **ภ.ง.ด.50 (ภาษีเงินได้นิติบุคคล) เสร็จ** — ปิด loop X4 ในงบการเงิน; PP30/ภ.ง.ด.3/53 ยังรอสเปก DBF |
| 12 | Compliance Calendar | ✅ เสร็จ | |
| 13 | Closing Period | ✅ เสร็จ | งวด/วันสิ้นงวดจาก ISPRD, close/reopen/lock (reopen/lock เฉพาะ Admin) |
| 14 | Audit Log | ✅ เสร็จ | viewer + ตัวกรอง (paginated) |
| 15 | Dashboard & KPI | ✅ เสร็จ (operational) | นับลูกค้า/งาน compliance/import — ยังไม่รวม KPI การเงิน |

**เสร็จ 11/15 + ภ.ง.ด.50** — ที่เหลือ (VAT/PP30, AR, AP, Payroll, Bank, ภ.ง.ด.3/53) รอสเปกโครงสร้างตาราง Express DBF

## Data Pipeline
```
Express DBF (ISINFO/GLACC/GLBAL/ISPRD)
   ↓ Import (StartExpressImportCommand)
Staging (StagingAccount / StagingTrialBalance) + AccountingPeriod (จาก ISPRD)
   ↓ Post (auto-post เมื่อสำเร็จ, หรือกดเอง) — ExpressPostingService
Production (Account + JournalEntry/Line)
   ↓
รายงาน: งบทดลอง / GL / งบการเงิน / ปิดรอบบัญชี / ภ.ง.ด.50
```
- **ภ.ง.ด.50**: กรอกภาษีเงินได้ (X4) + ภาษีจ่ายล่วงหน้า (WHT) ผ่าน FsExternalInput → ปิด loop ภาษีในงบดุล (ดู "ข้อจำกัด" ข้อ ภ.ง.ด.50)

## การ map ข้อมูล Express → ระบบ
- **GLACC → Account**: GROUP (1-5, Character) → AccountType; ACCTYP (0=detail/1=header) → IsPostable
- **GLBAL (CUR) → JournalEntry**: ยอดยกมา (`OPEN-{ปี}`, signed debit-positive) + ยอดเคลื่อนไหว (`MOVE-{ปี}`, รวม closing adjustment เพื่อให้ยอดสิ้นงวดตรง Express)
- **ISPRD → AccountingPeriod + ClosingPeriod**: วันเริ่ม/สิ้นงวด + LOCK → สถานะปิดงวด

## ข้อจำกัดที่ทราบ
- Express export เป็นยอดรวมรายปี → งบทดลอง/GL **รายปีถูกต้อง** แต่กรองรายเดือนภายในปีไม่แม่นยำ (movement กองที่ ธ.ค.)
- โมเดลการ post (OPEN-{ปี} ลงวันที่ 31 ธ.ค. ปีก่อน + MOVE-{ปี} ลงวันที่ 31 ธ.ค. ปีนั้น) **รองรับนำเข้าทีละ 1 ปีงบ/บริษัท** — รายงานคำนวณจาก "ยอดสะสมถึงสิ้นปีงบ" (= curEnd ของ Express). การนำเข้าหลายปีพร้อมกันจะทำให้ยอดสะสมซ้อนกัน (ต้องออกแบบ posting ใหม่ใน Phase 2)
- งบการเงินต้องตั้ง mapping บัญชี→บรรทัดงบก่อน (แท็บ Mapping) จึงจะแสดงผล — mapping เป็นข้อมูล**ต่อบริษัท** (ตั้งผ่าน UI/นำเข้า), Express GLACC ไม่มีฟิลด์ REF
- ~~ภาษีเงินได้ X4 ไม่มี counterpart ในงบดุล~~ **แก้แล้ว (ภ.ง.ด.50):** หน้า `/pnd50` ให้กรอกภาษีเงินได้ (X4) + ภาษีจ่ายล่วงหน้าที่นำมาหัก (WHT). งบดุลลง counterpart: ลด A4 (ภาษีจ่ายล่วงหน้า) ด้วย WHT, netPayable=X4−WHT → ">0 = ภาษีค้างจ่าย (TXP, หนี้สิน), <0 = ภาษีรอคืน (TXR, สินทรัพย์)". งบดุลสมดุลทุกกรณี; JSP CONNX (X4=WHT=47,470.04) → งบดุล 1,593,305.42 ตรงงบยื่นเป๊ะ
- ต้องอ่าน DBF ด้วย FileShare.ReadWrite (รองรับกรณี Express เปิดไฟล์ค้าง)

## สถานะล่าสุด (2026-06-02) — ✅ ตรวจความถูกต้องผ่านแล้ว
นำเข้า JSIT2016 (= JSP CONNX) ปี 2025 และตรวจเทียบกับ**งบการเงินจริงที่ยื่น** (reference/financial):
- **Baseline ครบถูกต้อง**: Accounts 251 (postable 200), JournalEntries 2, lines 164, AccountingPeriods 24, posted 1; งบทดลอง ยอดยกมา DR=CR 6,944,419.59, เคลื่อนไหว DR=CR 20,667,109.83, signed net รวม = 0 (สมดุล)
- **งบกำไรขาดทุน: ตรงงบจริงทุกบรรทัด** — รายได้รวม 3,579,157.45, ต้นทุนขาย 2,238,451.94, กำไรก่อนภาษี 566,641.62, กำไรสุทธิหลังภาษี 519,171.58 (ภาษี X4 = 47,470.04)
- **งบดุล: สมดุลเป๊ะจาก GL** — สินทรัพย์ = หนี้สิน+ทุน = 1,640,775.46 (เมื่อไม่ใส่ X4)

### Bug ที่พบและแก้ระหว่างตรวจ (5 จุด)
1. `StatementLineSeed` เป็น dead code (ไม่ถูกเรียกใน OnModelCreating) → ตาราง StatementLines ว่าง. แก้: seed ตอน startup (idempotent) ใน `DbInitializer`
2. `GetBalanceSheetQueryHandler` ใช้ช่วงวันที่ใน-ปี → งบดุลได้แค่ยอดเคลื่อนไหว ไม่รวมยอดยกมา. แก้: ใช้ยอดสะสมถึงสิ้นปีงบ
3. `GetProfitLossQueryHandler` (รายปี) ใช้ within-year → รายได้/ค่าใช้จ่ายขาดส่วนที่ offset ในยอดยกมา. แก้: รายปีใช้ยอดสะสมถึงสิ้นปีงบ
4. `FinancialStatementEngine.BuildProfitLoss` นับ "C" (ต้นทุนขาย) ซ้ำใน expLines. แก้: exclude "C"
5. RE ใช้ยอด*ก่อนปี* ของบัญชี 32000 → พลาด closing adjustment ที่อยู่ใน MOVE. แก้: ใช้ยอดสะสมถึงสิ้นปีของบัญชี RE

## สถานะล่าสุด (เพิ่มเติม) — โมดูล ภ.ง.ด.50 เสร็จ
ปิด loop ภาษีเงินได้ในงบการเงิน: หน้า `/pnd50` (เมนูภาษี) + `GET /financial-statement/external-inputs` + FsExternalInput รับ `X4`/`WHT` + StatementLines TXR/TXP. งบดุลลง counterpart และสมดุลทุกกรณี — verified end-to-end (API 3 กรณี + หน้า UI จริง). JSP CONNX → งบดุล 1,593,305.42 ตรงงบยื่นเป๊ะ

## งานถัดไป
1. รอสเปก/ตัวอย่างตาราง DBF ของ VAT/sales/purchase/payroll → สร้าง entity + import + report ของโมดูลที่เหลือ (VAT/PP30, AR, AP, Payroll, Bank, ภ.ง.ด.3/53)
2. (ตัวเลือก) อ่านยอดรายเดือนจาก GLBAL (DEBIT1..12) เพื่อให้งบทดลอง/P&L รายเดือนแม่นยำ
3. (ตัวเลือก) กลไกตั้ง/นำเข้า account→REF mapping ต่อบริษัท (ปัจจุบันตั้งผ่าน UI ทีละบัญชี)
4. (Phase 2) ออกแบบ posting ใหม่รองรับนำเข้าหลายปีงบพร้อมกัน

---

## สถานะเทียบ Requirement v11 (workbook 2025_JSPC_FIN.xlsx) — อัปเดต 2026-06-04

Requirement v11 เพิ่มขอบเขตจาก workbook ปิดงบจริง พร้อมคำตอบ User ยืนยันแล้ว (open questions ปิดแล้ว 12/13 ข้อ)

| โมดูล (req v11) | สถานะปัจจุบัน | บล็อก DBF? | หมายเหตุ |
|---|---|---|---|
| Adjusted TB + Adjustment Entry | ✅ เสร็จ (2026-06-04) | ไม่ | AdjustmentEntry/Line + เมนู "กระดาษทำการปิดงบ" `/adjustments` — ดูหัวข้อด้านล่าง |
| Leasing / Loan Working Paper | ❌ ยังไม่เริ่ม | ไม่ | **มีหน้าจัดการในระบบ → ส่ง adjustment เข้า TB ปีปัจจุบัน** |
| CAP (งบเปลี่ยนแปลงส่วนผู้ถือหุ้น) | ❌ ยังไม่เริ่ม | ไม่ | FS ปัจจุบัน = BS + P&L |
| NOTE2 (หมายเหตุประกอบงบ) | ❌ ยังไม่เริ่ม | ไม่ | แยก template ↔ data binding (docs/13) |
| DBD group-code taxonomy | 🟡 มี StatementLines ต่อบริษัท | ไม่ | ยังไม่มี master taxonomy มาตรฐาน |
| Fixed Asset Register | ❌ ยังไม่เริ่ม | ไม่ | ค่าเสื่อม 2 ชุด + disposal + กำไร/ขาดทุน auto (docs/14) |
| Prepaid Schedule | ❌ ยังไม่เริ่ม | ไม่ | pattern เดียวกลาง (docs/14) |
| Stock / FIFO / FG↔TB | ❌ ยังไม่เริ่ม | บางส่วน | FIFO จาก Express; ต่าง→adjustment manual (docs/15) |
| Cash Count / Interest Income | ❌ ยังไม่เริ่ม | ไม่ | docs/13, docs/17 |
| AR/AP Recon + Bank Statement | ⛔ รอสเปก DBF + bank statement | ใช่ | status matched/partial/unmatched (docs/17) |
| Subsequent Payment Check | ❌ ยังไม่เริ่ม | ไม่ | ใช้ GL1/JV1 ปีถัดไป (docs/17) |
| TAX engine (เต็มรูป) | 🟡 ภ.ง.ด.50 (X4/WHT) เสร็จ | ไม่ | ต่อยอดเป็น full engine จาก TB (docs/16) |
| PP30 auto จาก VAT | 🟡 อยู่ในโมดูล Tax | ใช่ | รอสเปก Input/Output VAT DBF |
| PND.3/53 PDF reconcile | ❌ ยังไม่เริ่ม | ไม่ (ใช้ PDF) | layout คงที่ → template parser (docs/16) |
| Field-level audit ทุก field | 🟡 มี Audit Log พื้นฐาน | ไม่ | ยังไม่ field-level (docs/18) |
| Attachment / Evidence | ❌ ยังไม่เริ่ม | ไม่ | docs/18 |
| Audit log export | 🟡 มี viewer/filter | ไม่ | ยังไม่มี export Excel/PDF/CSV |
| Report package (draft/review/final/lock) | 🟡 มี Closing Period lock | ไม่ | ยังไม่มี report package/version |
| Snapshot Express ตามรอบปิดงบ | ❌ ยังไม่เริ่ม | ไม่ | เก็บถาวร 10 ปี |

### ✅ Adjusted TB + Adjustment Entry — เสร็จ (2026-06-04, req v11 docs/13 ข้อ 1–2)
ชั้นฐานของกระดาษทำการปิดงบ ที่โมดูลปิดงบอื่น (Leasing/Loan/FixedAsset/Prepaid/Stock) จะส่ง adjustment เข้ามาต่อยอด

- **Domain:** `AdjustmentEntry` (header: ClientCompanyId, FiscalYear, DocumentNo `ADJ-{ปี}-{ลำดับ}`, EntryDate, SourceType, Reference, Reason, AttachmentPath) + `AdjustmentEntryLine` (AccountId, Debit, Credit, Description) + enum `AdjustmentSourceType` (Manual/Leasing/Loan/Tax/Other). migration `AddAdjustmentEntries`
- **CQRS:** Create/Update/Delete (balanced Dr=Cr บังคับใน validator; account ต้อง postable+อยู่บริษัทเดียวกัน; ทุกคนลบได้+audit ตาม req #7) + `GetAdjustmentEntriesQuery` (list) + `GetAdjustedTrialBalanceQuery`
- **Adjusted TB:** คำนวณสดจากยอดนำเข้า (JournalEntry, สะสมถึงสิ้นปีงบ) + adjustment ปัจจุบัน — คอลัมน์: ยอดยกมา / เคลื่อนไหว / ก่อนปรับ(net) / ปรับปรุง / หลังปรับ(net) ตามสูตร `DebitFinal = max(BalDr+AdjDr−BalCr−AdjCr,0)`. มีธง balancedBefore/adjustmentsBalanced/balancedAfter (VR)
- **API:** `AdjustmentsController` — `GET/POST/PUT/DELETE /api/v1/adjustments` + `GET /adjustments/trial-balance`
- **Frontend:** เมนู "กระดาษทำการปิดงบ" → `/adjustments` 2 แท็บ (งบทดลองหลังปรับปรุง / รายการปรับปรุง + ฟอร์ม modal)
- **Verify:** ทดสอบ API ครบ (create ADJ-2025-0001 / reject imbalanced 422 / สูตรถูก acc4 73860+1000=74860, acc8 3280.88−1000=2280.88 / delete 204) + หน้า UI จริงผ่าน Preview กับ JSIT2016 ปี 2025 (ยอดก่อนปรับ Dr=Cr 9,078,861.69, badges เขียวครบ)
- **หมายเหตุ enum:** API serialize enum เป็น **integer** (ไม่มี JsonStringEnumConverter — closing-period/compliance พึ่ง numeric status) → frontend types ของโมดูลนี้ใช้ number, POST sourceType เป็นตัวเลข

### คำตอบ Open Questions ที่ใช้เป็นฐานพัฒนา (ยืนยัน 2026-06-04)
1. "เชื่อม DB Express" = **อ่านไฟล์ DBF โดยตรง** (ที่ทำอยู่) → ไม่มีความขัดแย้งกับ pipeline ปัจจุบัน
2. Payroll = **เลื่อนไป Phase หลัง**
3. ขายสินทรัพย์ = ระบบ**คำนวณกำไร/ขาดทุนอัตโนมัติ**
4. อัตราค่าเสื่อม = **มี master ต่อประเภทสินทรัพย์** (default + override)
5. Stock ≠ TB = **แสดงผลต่าง ให้บัญชีบันทึก adjustment เอง** (ไม่ auto)
6. Parallel run = **ตรงเป๊ะทุกยอด (ผลต่าง = 0)**
7. การลบข้อมูล = **ทุกคนลบได้ + audit trail**
8. Export = **Excel + PDF + CSV**
9. review/lock งบก่อน final = **เก็บ version final เริ่มจาก v0; ยื่นงบแล้ว lock ห้ามแก้; ยื่นเพิ่มเติม = เปิด version ใหม่ (version เดิม freeze ถาวร); ปลดล็อกได้ทุกคนที่มีสิทธิ์ในบริษัท + audit** (ดู docs/18)
10. Express data = **snapshot ตามรอบปิดงบ + เก็บถาวร**
11. Leasing/Loan = **มีหน้าจัดการในระบบ** → adjustment เข้า TB ปีปัจจุบัน
12. PDF สรรพากร = **layout คงที่ → template parser**
13. Retention = **10 ปี**
