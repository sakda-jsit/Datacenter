# Implementation Status

สถานะการพัฒนา Phase 1 (อัปเดต: 2026-06-02)

## โมดูล (Phase 1)

| # | โมดูล | สถานะ | หมายเหตุ |
|---|---|---|---|
| 1 | Client Management | ✅ เสร็จ | full-stack |
| 2 | Import Data (Express DBF) | ✅ เสร็จ | ISINFO/GLACC/GLBAL/ISPRD + auto-post |
| 3 | VAT Management | ✅ เสร็จ (2026-06-05) | นำเข้า ISVAT.DBF + รายงาน ภ.พ.30 รายเดือน + รายละเอียดภาษีซื้อ/ขาย — ดูหัวข้อด้านล่าง |
| 4 | AR Management | ⛔ รอสเปก DBF | ต้องการตารางลูกหนี้/ใบแจ้งหนี้ |
| 5 | AP Management | ⛔ รอสเปก DBF | ต้องการตารางเจ้าหนี้/ใบรับวางบิล |
| 6 | Payroll | ⛔ รอสเปก DBF | ต้องการข้อมูลเงินเดือน + สูตรภาษี/ประกันสังคม |
| 7 | Bank Reconciliation | ⛔ รอสเปก | ต้องการ statement ธนาคาร |
| 8 | Trial Balance | ✅ เสร็จ | อ่านจาก Account + JournalEntry (ต้อง post ก่อน) |
| 9 | General Ledger | ✅ เสร็จ | running balance ต่อบัญชี |
| 10 | Financial Statement | ✅ เสร็จ | งบฐานะการเงิน + กำไรขาดทุน (ต้องตั้ง mapping บัญชี→บรรทัดงบ) |
| 11 | Tax Report (PP30/PND) | ✅ เสร็จ (2026-06-05) | **ภ.ง.ด.50** (X4 loop) + **ภ.พ.30** (ISVAT, โมดูล #3) + **ภ.ง.ด.3/53** (ISTAX) — ครบชุดแบบภาษีหลัก |
| 12 | Compliance Calendar | ✅ เสร็จ | |
| 13 | Closing Period | ✅ เสร็จ | งวด/วันสิ้นงวดจาก ISPRD, close/reopen/lock (reopen/lock เฉพาะ Admin) |
| 14 | Audit Log | ✅ เสร็จ | viewer + ตัวกรอง (paginated) |
| 15 | Dashboard & KPI | ✅ เสร็จ (operational) | นับลูกค้า/งาน compliance/import — ยังไม่รวม KPI การเงิน |

**เสร็จ 13/15 + ภ.ง.ด.50 + ภ.พ.30 + ภ.ง.ด.3/53** — ที่เหลือ (AR, AP, Payroll, Bank) ปลดบล็อกได้โดยสำรวจ DBF จริง: AR* (ARMAS/ARTRN/ARBIL), AP* (APMAS/APTRN/APBIL) มีอยู่ใน Express แล้ว — ใช้ `tools/dbf_diag.py` reverse-engineer โครงสร้าง

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
| Leasing / Loan Working Paper | ✅ เสร็จ (2026-06-04) | ไม่ | LeaseContract + engine effective-interest + เมนู "เช่าซื้อ / เงินกู้" `/leasing` → generate adjustment เข้า TB — ดูหัวข้อด้านล่าง |
| CAP (งบเปลี่ยนแปลงส่วนผู้ถือหุ้น) | ❌ ยังไม่เริ่ม | ไม่ | FS ปัจจุบัน = BS + P&L |
| NOTE2 (หมายเหตุประกอบงบ) | ❌ ยังไม่เริ่ม | ไม่ | แยก template ↔ data binding (docs/13) |
| DBD group-code taxonomy | 🟡 มี StatementLines ต่อบริษัท | ไม่ | ยังไม่มี master taxonomy มาตรฐาน |
| Fixed Asset Register | ✅ เสร็จ (2026-06-05) | ไม่ (import ได้) | FixedAsset + AssetTypeMaster + DepreciationEngine (เส้นตรง 2 ชุด) + disposal กำไร/ขาดทุน auto + **import จาก Express FAMAS.DBF** + เมนู "สินทรัพย์ถาวร" `/fixed-assets` → generate adjustment เข้า TB — ดูหัวข้อด้านล่าง |
| Prepaid Schedule | ❌ ยังไม่เริ่ม | ไม่ | pattern เดียวกลาง (docs/14) |
| Stock / FIFO / FG↔TB | ❌ ยังไม่เริ่ม | บางส่วน | FIFO จาก Express; ต่าง→adjustment manual (docs/15) |
| Cash Count / Interest Income | ❌ ยังไม่เริ่ม | ไม่ | docs/13, docs/17 |
| AR/AP Recon + Bank Statement | ⛔ รอสเปก DBF + bank statement | ใช่ | status matched/partial/unmatched (docs/17) |
| Subsequent Payment Check | ❌ ยังไม่เริ่ม | ไม่ | ใช้ GL1/JV1 ปีถัดไป (docs/17) |
| TAX engine (เต็มรูป) | 🟡 ภ.ง.ด.50 (X4/WHT) เสร็จ | ไม่ | ต่อยอดเป็น full engine จาก TB (docs/16) |
| PP30 auto จาก VAT | ✅ เสร็จ (2026-06-05) | ไม่ (ISVAT) | นำเข้า ISVAT.DBF → รายงาน ภ.พ.30 รายเดือน — ดูหัวข้อด้านล่าง |
| PND.3/53 จาก ISTAX | ✅ เสร็จ (2026-06-05) | ไม่ (ISTAX) | นำเข้า ISTAX.DBF → รายงาน ภ.ง.ด.3/53 รายเดือน + รายละเอียด — ดูหัวข้อด้านล่าง (ไม่ต้องพึ่ง PDF) |
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

### ✅ Leasing / Loan Working Paper — เสร็จ (2026-06-04, req v11 docs/13 ข้อ 2)
ต่อยอดจาก Adjustment Entry: บันทึกสัญญา → ระบบคำนวณตารางตัดบัญชี → generate รายการปรับปรุงดอกเบี้ยเข้า TB

- **Domain:** `LeaseContract` (เงินต้น/ค่างวด/จำนวนงวด/วันเริ่ม/VAT-ต่องวด + ผูกบัญชี GL 4 ตัว: หนี้สิน/ดอกเบี้ยรอตัด/ภาษีซื้อยังไม่ถึงกำหนด/ดอกเบี้ยจ่าย) + enum `LeaseContractType` (HirePurchase/Loan). migration `AddLeaseContracts`. **ไม่เก็บ schedule — คำนวณสด** (เหมือน Adjusted TB)
- **Engine:** `LeaseAmortizationEngine` (pure) — solve อัตราต่องวดแบบ effective-interest (Newton-Raphson) จาก (เงินต้น, ค่างวด, จำนวนงวด); ดอกเบี้ยงวด = round(ยอดคงเหลือ × r); งวดสุดท้าย absorb เศษ → ยอดเงินต้นรวม = financed เป๊ะ. สรุปสิ้นปี: ยอดยกมา/ชำระในปี/คงเหลือ/current portion(1 ปี)/ระยะยาว ต่อ gross liability, ดอกเบี้ยรอตัด, VAT, net principal
- **CQRS:** Create/Update/Delete สัญญา + `GenerateLeaseAdjustment` (ดอกเบี้ยรับรู้ในปี → Dr ดอกเบี้ยจ่าย / Cr ดอกเบี้ยรอตัด [HP] หรือ Cr หนี้สิน [Loan] ผ่าน `CreateAdjustmentEntryCommand` เดิม) + Queries: list / detail+schedule / `GetLeaseWorkpaper` (SUM ต่อสัญญา + เทียบ GL → Diff)
- **API:** `LeasingController` — `GET/POST/PUT/DELETE /api/v1/leasing` + `/leasing/{id}` (schedule) + `/leasing/workpaper` + `/leasing/generate-adjustment`
- **Frontend:** เมนู "เช่าซื้อ / เงินกู้" → `/leasing` 2 แท็บ (สัญญา + ฟอร์ม/ตารางตัดบัญชี modal / กระดาษทำการ + เทียบ GL + ปุ่ม generate adjustment)
- **Verify:** สร้างสัญญา SOLAR ROOFTOP (E057-2025) จากไฟล์จริง `2025_JSPC_LEASING.xlsx` กับ JSIT2016 — **ยอด gross liability + VAT ตรง sheet SUM เป๊ะ** (164,304 / ชำระ 3,912 / คงเหลือ 160,392 / current 23,472 / LT 136,920; VAT 10,748.64/255.92/1,535.52); ดอกเบี้ยแยกรายงวดต่างจาก Excel ≤0.02 สตางค์ (วิธีปัดเศษ IRR), ยอดเงินต้นปิดที่ 0.00 เป๊ะ. generate → ADJ-2025-0001 (Dr ดอกเบี้ยจ่าย 1,840.49 / Cr ดอกเบี้ยรอตัด 1,840.49) เข้า Adjustment module + adjusted TB สมดุล. UI ผ่าน Preview ครบ
- **หมายเหตุความแม่น:** engine สร้างยอดสอดคล้องภายในสัญญา (รวมเป๊ะ); การ split รายงวดอาจต่าง Excel ต้นทาง ≤ ไม่กี่สตางค์ → ปรับ manual ได้ที่หน้า adjustment

### ✅ Fixed Asset Register — เสร็จ (2026-06-05, req v11 docs/14)
ทะเบียนสินทรัพย์ถาวร (source of truth) + ค่าเสื่อมราคา 2 ชุด (บัญชี/ภาษี) + จำหน่าย/ขาย กำไร/ขาดทุนอัตโนมัติ → generate adjustment เข้า TB

- **Domain:** `AssetTypeMaster` (มาสเตอร์ประเภท + อัตราบัญชี/ภาษี + อายุ, global, seed 8 ประเภทมาตรฐาน) + `FixedAsset` (รหัส/ชื่อ/ประเภท/วันได้มา/ราคาทุน/มูลค่าซาก/อัตราบัญชี+ภาษี override ได้/สถานะ Active·Disposed·Sold·WrittenOff/วันจำหน่าย+ราคาขาย + ผูกบัญชี GL: ค่าเสื่อมสะสม/ค่าเสื่อมราคา/สินทรัพย์). enums `FixedAssetStatus`, `DepreciationSet`(Book/Tax); `AdjustmentSourceType.FixedAsset=5`. migration `AddFixedAssets`. **ไม่เก็บ schedule — คำนวณสด**
- **Engine:** `DepreciationEngine` (pure) — เส้นตรง: ปีเต็ม = round(ราคาทุน×อัตรา%, 2), ฐาน = ราคาทุน−มูลค่าซาก (cap สะสมไม่เกินฐาน, ปีสุดท้าย absorb เศษ → NBV ปิดที่มูลค่าซากเป๊ะ), ปีแรก/ปีจำหน่าย prorate ตามวันใช้งานจริง/วันทั้งปี, อัตรา 0 = ไม่คิด (ที่ดิน). `AsOf(fiscalYear)` คืน opening/charge/closing/NBV; `Disposal()` = ราคาขาย − NBV ชุดบัญชี ณ วันจำหน่าย
- **CQRS:** Create/Update(รวมจำหน่าย)/Delete สินทรัพย์ + `GenerateDepreciationAdjustment` (เลือกชุด Book/Tax → Dr ค่าเสื่อมราคา / Cr ค่าเสื่อมราคาสะสม ผ่าน `CreateAdjustmentEntryCommand`) + Queries: `GetAssetTypes` (global) / list / detail+schedule 2 ชุด+disposal / `GetFixedAssetWorkpaper` (per-asset + สรุปตามประเภท[NOTE2] + เทียบ GL: accum เทียบยอดสะสมสิ้นปี, expense เทียบ movement ในปี)
- **API:** `FixedAssetsController` — `GET /fixed-assets/asset-types` + `GET/POST/PUT/DELETE /api/v1/fixed-assets` + `/fixed-assets/{id}` + `/fixed-assets/workpaper` + `/fixed-assets/generate-adjustment`
- **Frontend:** เมนู "สินทรัพย์ถาวร" (ไอคอน building) → `/fixed-assets` 2 แท็บ (ทะเบียน + ฟอร์ม modal auto-fill อัตราจากประเภท + ตารางค่าเสื่อม 2 ชุด modal / กระดาษทำการ + สรุปประเภท + เทียบ GL + ปุ่ม generate ตามชุด)
- **Verify:** ทดสอบ engine กับสินทรัพย์ที่รู้ผล — รถ 120,000 อัตรา 20% ได้มา 2024-07-01 → 2024 prorate 184/366 = 12,065.57, 2025 = 24,000, สะสม 36,065.57, NBV 83,934.43, ปีสุดท้าย 2029 absorb เศษปิด NBV 0.00 เป๊ะ; disposal รถขาย 100,000 อัตรา 20% ขาย 2025-06-30 ราคา 50,000 → ขาดทุน 82.19 (NBV 50,082.19); generate → ADJ-2025-0001 (source=FixedAsset) Dr ค่าเสื่อม / Cr ค่าเสื่อมสะสม สมดุล 33,917.81. API ครบ + UI ผ่าน Preview (ทะเบียน/modal 2 ชุด/workpaper/เทียบ GL) ไม่มี console error
- **หมายเหตุ:** ใช้ตัดจำหน่ายแบบเส้นตรงเท่านั้น (req v11 = DATEDIF/straight-line); ยังไม่ generate journal ตัดจำหน่ายสินทรัพย์ออก (ลบ cost+accum+กำไร/ขาดทุน) — ตอนนี้แสดงกำไร/ขาดทุนให้บันทึก adjustment เองได้ที่หน้า adjustment

#### ✅ Import จาก Express FAMAS.DBF — เสร็จ (2026-06-05)
Express มีทะเบียนสินทรัพย์เต็มรูปใน **`FAMAS.DBF`** (ตารางมาตรฐานทุกบริษัท) → import ได้ ไม่ต้องป้อนมือ

**หลักการสถาปัตยกรรม (User ยืนยัน 2026-06-05):** การนำเข้าข้อมูลจาก Express **ทำที่เดียว** = เมนู ข้อมูลและนำเข้า → "นำเข้าข้อมูล" (StartExpressImport) ซึ่งดึง **ทุกอย่าง**ในครั้งเดียว (GL/บัญชี/รอบบัญชี + **สินทรัพย์ FAMAS**). ไม่มีปุ่ม import แยกตามหน้าย่อย. ระบบเน้นดึงจาก Express ให้มากที่สุด ป้อนมือเฉพาะที่ Express ไม่มี (เช่น การแมพบัญชี GL)
- **field map:** FASCOD→AssetCode, FASDES+FASDES2→AssetName, FASGRP→AssetGroupCode, ACCCOD→CategoryCode, STRDAT/PURDAT→AcquireDate, COSVAL→Cost, SALVAG→SalvageValue (Express ใช้ 1 บาท), RATE→Book+TaxRatePct, **ACCMBF→AccumulatedBroughtForward** (ค่าเสื่อมสะสมยกมา), SALDAT/SALAMT/→Disposal
- **ยอดยกมา (ACCMBF):** field `AccumulatedBroughtForward` + `BroughtForwardYear` ใน FixedAsset → engine เริ่มสะสมจากยอดนี้ ณ ต้นปีที่ระบุ (แทนคำนวณใหม่จากวันได้มา) → **ตรง Express เป๊ะ** (verify ACS2023: ค่าเสื่อมสะสมตาม schedule vs GL ที่ Express โพสต์ ต่างเพียง 0.20–0.47 บาท)
- **GL mapping (สิ่งเดียวที่ป้อนมือ):** Express เก็บแค่หมวด (ACCCOD) ไม่เก็บเลขบัญชีจริง → entity `AssetAccountMapping` (ClientCompanyId, CategoryCode, 3 บัญชี GL) + หน้าแมพ (modal "แมพบัญชี" ในหน้าสินทรัพย์) ที่ list หมวดทั้งหมด (รวมที่ยังไม่แมพ Id=0) → บันทึกแล้วเติมบัญชีให้สินทรัพย์ที่ยังว่างอัตโนมัติ; ครั้งถัดไป import กลางก็เติมบัญชีจาก mapping ให้เอง
- **โครงสร้าง:** `FixedAssetImporter.ImportAsync` (service กลาง, upsert ตาม AssetCode, ไม่ผ่าน staging) ถูกเรียกใน `StartExpressImportCommandHandler` (หลัง auto-post, try/catch แยก). `IExpressDbfAdapter.ReadFixedAssetsAsync` (FAMAS optional → ว่างถ้าไม่มี). **ไม่มี** command/endpoint import แยกของ FA
- **API ที่เหลือของ FA:** `GET/PUT /fixed-assets/account-mappings` (แมพบัญชี) — import ใช้ `POST /api/v1/import/express` กลาง
- **Frontend (เฟสนี้):** ทะเบียนสินทรัพย์ **ดึงจาก Express 100%** — หน้าสินทรัพย์มีเฉพาะ "แมพบัญชี" (สิ่งเดียวที่ป้อนมือ) + ดู/แก้ไข/ลบ; **ปิดการสร้างสินทรัพย์เองในระบบ** (เอาปุ่ม + create path ใน UI ออก; backend CreateFixedAssetCommand/POST ยังคงอยู่ dormant สำหรับเฟสหน้า)

#### ✅ ปิด gap NOTE2 + ตัดจำหน่าย (2026-06-05)
- **สรุปตามประเภท (NOTE2):** workpaper เดิมกองรวม "(ไม่ระบุประเภท)" เพราะ import ไม่ map FASGRP→AssetType. แก้: `GetFixedAssetWorkpaperQueryHandler` จัดกลุ่มตาม **หมวด ACCCOD** (CategoryCode) + ใช้ชื่อจาก `AssetAccountMapping.Description` (เช่น "คอมพิวเตอร์") → fallback รหัสหมวด. Verify JSP CONNX: 6–7 กลุ่ม (CO/EQ/FU/SO...) พร้อม cost/charge ต่อกลุ่ม. รายการในทะเบียนแสดง categoryCode ในคอลัมน์ประเภทด้วย
- **Generate รายการตัดจำหน่าย/ขาย:** `GenerateDisposalAdjustmentCommand` + `POST /fixed-assets/generate-disposal` + modal "สร้างรายการตัดจำหน่าย/ขาย" (เลือกสินทรัพย์ที่จำหน่ายในปีงบ + บัญชีกำไร/ขาดทุน/เงินรับ). สร้าง AdjustmentEntry: **Dr ค่าเสื่อมสะสม + Dr เงินรับ(ราคาขาย) + Dr ขาดทุน / Cr ราคาทุน + Cr กำไร** สมดุลเสมอ. Verify JSP CONNX ปี 2025: 2 รายการ (C003 กำไร 2,335.45 + E037 กำไร 3,712.47) → ADJ สมดุล Dr=Cr 38,142.24
- **Bug fix (brought-forward ตัดหมดแล้ว):** สินทรัพย์ที่ ACCMBF = ฐานคิดค่าเสื่อม (ตัดหมดตั้งแต่ยอดยกมา) → `BuildSchedule` คืน 0 แถว → `AsOf`/`Disposal` เดิม fallback accum=0/NBV=ราคาทุนเต็ม **ผิด**. แก้: เพิ่ม `StartingAccum()` ให้ทั้งสอง method fallback เป็นยอดยกมา → C003 NBV ณ จำหน่าย = 1.00 (ซาก), accum = 19,112 ถูกต้อง
- **Verify:** import กลาง ACS2023/2025 → batch message รวม "สินทรัพย์ 6 รายการ (ใหม่ 6)"; 6 สินทรัพย์ (ISUZU D-MAX/ตู้เย็น/แอร์/iPad/ตู้เชื่อม/มอเตอร์ไซค์), brought-forward ตรง, เติมบัญชีจาก mapping อัตโนมัติ, workpaper+generate+UI ผ่าน Preview ครบ

### ✅ Export รายงาน (Excel/CSV/PDF) — เสร็จ (2026-06-05, req v11 #8)
Utility กลางฝั่ง frontend + ปุ่ม `ExportMenu` ใช้ซ้ำทุกตาราง (เลี่ยงปัญหาฟอนต์ไทยใน PDF ฝั่ง server)
- **`shared/utils/exportTable.ts`:** `exportCsv` (UTF-8 + BOM), `exportXlsx` (SheetJS `xlsx` 0.20.3 จาก CDN ทางการ — 0 vulnerabilities; 1 section = 1 sheet), `exportPdf` (เปิดหน้าต่างพร้อม `window.print()` → Save as PDF; ไทยเรนเดอร์ผ่านฟอนต์ระบบ Sarabun/Tahoma). รับ `ExportSection[]` (รายงานหลายส่วน เช่น BS+P&L, FA workpaper 3 ส่วน)
- **`shared/components/ui/ExportMenu.tsx`:** dropdown "ส่งออก ▾" (Excel/CSV/PDF) + `getSections()` lazy build
- **ติดตั้งครบทุกตารางที่มีข้อมูลจริง:** งบทดลอง, บัญชีแยกประเภท (flatten lines), งบการเงิน (BS 3 ส่วน + P&L), งบทดลองหลังปรับปรุง, **รายการปรับปรุง (entries)**, สินทรัพย์ถาวร (ทะเบียน + workpaper 3 ส่วน), เช่าซื้อ/เงินกู้ workpaper, **ปฏิทินงาน (compliance tasks)**, **ประวัตินำเข้า**, **ปิดรอบบัญชี (สถานะ 12 งวด)**, ประวัติการใช้งาน, รายชื่อลูกค้า (รายการ paginated = export หน้าปัจจุบัน)
- **Verify:** CSV ได้ UTF-8 ไทยถูก (title/subtitle/section/headers + data), Excel ได้ .xlsx zip valid (PK magic, MIME ถูก), typecheck ผ่าน, render ปกติ — ทดสอบจริงทั้ง FA workpaper + ประวัตินำเข้า
- **เหลือเฉพาะโมดูล stub** (VAT/AR/AP/Payroll/Bank) ที่ยังไม่มีข้อมูลจริง — เพิ่ม `ExportMenu` ได้ทันทีเมื่อโมดูลพร้อม; รายงานภาษี = ComingSoon, ภ.ง.ด.50 เป็นฟอร์ม (ไม่ใช่ตาราง)

### ✅ ชื่อทางการบริษัท + master upsert (2026-06-05)
แยกชื่อ Express ออกจากชื่อทางการ + เปลี่ยน import เป็น upsert (ไม่ลบทิ้ง) — รองรับเปลี่ยนชื่อ/ที่อยู่บริษัท
- **ClientCompany เพิ่ม:** `LegalName` (ชื่อทางการ ใช้ออกงบ/แสดงทั้งระบบ — แก้ได้, import **ไม่ทับ**), `EnglishName`, sync `Address` จาก ISINFO; `Name` = ชื่อ Express (sync ทับได้ = reference). unique business key `(TaxId, BranchCode)` filtered `[TaxId]<>'' AND [IsActive]=1`; PK ยังเป็น `Id`
- **Migration `AddClientLegalNameAndProfile`:** backfill `LegalName=Name`; dedup รายการ active ที่ taxid+branch ซ้ำ (คงตัวที่มีข้อมูล/ไม่ใช่สำเนา, soft-deactivate ตัวที่เหลือ — เคส COPY1 "บริษัท TEST" ปิด, MILLIONT คงไว้); สร้าง unique index
- **Import = upsert profile:** `StartExpressImport` อ่าน ISINFO refresh `Name/Address/EnglishName` (sync) **คง LegalName**; backfill LegalName ถ้าว่าง; ลง audit `SyncProfile`. ExpressDbfAdapter normalize `\xa0`→space; อ่าน ADDR02/ADDR01
- **Master upsert:** AccountingPeriods เปลี่ยน wholesale-replace → upsert by (Year,PeriodNo); Accounts upsert อยู่แล้ว (found→update/ไม่ลบ). ExpressDatasetFilter ตัด `COPY*`/X-/Z- (path) เพิ่ม
- **Display ทั้งระบบ → LegalName:** ~14 handlers (TB/GL/FS/AdjustedTB/FA/Leasing/ClosingPeriod/Compliance/Import/Audit/Dashboard) + ClientList(name=LegalName) + ClientDetail(เพิ่ม legalName) + ฟอร์มแก้ไข edit LegalName (โชว์ชื่อ Express เป็น ref เมื่อต่างกัน)
- **Historical (snapshot ชื่อ/ที่อยู่ต่อปีที่ล็อกงบ): เลื่อนไป report package (docs/18)** — ออกแบบ schema รองรับแล้ว (LegalName เป็นตัวตั้งต้น snapshot)
- **Verify:** backfill 72/72, dedup COPY1 ปิด, unique index, แก้ LegalName→report แสดงชื่อใหม่, re-import คง LegalName + sync Name (normalize), edit form + client list ผ่าน Preview ไม่มี error

### ✅ VAT / ภ.พ.30 — เสร็จ (2026-06-05, โมดูล #3 + req v11 "PP30 auto จาก VAT")
นำเข้ารายงานภาษีซื้อ/ขายจาก Express **ISVAT.DBF** → ออกรายงาน ภ.พ.30 รายเดือน + รายละเอียดรายใบกำกับภาษี

- **Express ISVAT.DBF (ทุกบริษัทที่จด VAT):** `VATREC` แยก **S=ภาษีขาย(Output)** / **P=ภาษีซื้อ(Input)**, `VATPRD`=เดือนภาษี (งวด ภ.พ.30), `AMT01+AMT02`=ฐานภาษี, `VAT01+VAT02`=ภาษี, `AMTRAT0`=ยอดอัตรา 0%, `DOCNUM/DOCDAT/REFNUM/DESCRP/TAXID/PRENAM/LATE`. ตรวจ: ฐาน×7% ตรง VAT01 เป๊ะ
- **Domain:** `VatEntry` (transactional, ดึงจาก Express 100% ไม่แก้มือ) + enum `VatEntryType` (Output=1/Input=2). migration `AddVatEntries`. config: VatType เก็บเป็น **string** (`HasConversion<string>`) → ห้าม cast `(int)` ใน LINQ-to-SQL projection (materialize ก่อนแล้ว map ใน memory)
- **Import:** `IExpressDbfAdapter.ReadVatEntriesAsync` (ISVAT optional → ว่างถ้าไม่จด VAT, เฉพาะ VATREC S/P) + `VatEntryImporter.ImportAsync` (แทนที่ทั้งชุดต่อบริษัท = sync ใหม่ทุก import คล้าย JournalEntry/batch) เรียกใน `StartExpressImport` (try/catch แยก, audit `ImportVat`). **ไม่มีปุ่ม import แยก** (หลักการนำเข้ากลาง)
- **CQRS/API:** `GetVatReportQuery` (ภ.พ.30 รายเดือน ม.ค.–ธ.ค. + ยอดรวมทั้งปี, NetVat=ภาษีขาย−ภาษีซื้อ) / `GetVatEntriesQuery` (รายละเอียด: ตัวกรองเดือน+ประเภท) / `GetVatYearsQuery` (ปีที่มีข้อมูล). `VatController` — `GET /vat/report` · `/vat/years` · `/vat`
- **Frontend:** เมนู "ภาษีมูลค่าเพิ่ม" → `/vat` 2 แท็บ (ภ.พ.30 รายเดือน + รายละเอียดภาษีซื้อ/ขาย) + ปีจาก /vat/years (auto เลือกปีล่าสุด) + ExportMenu ทั้งสองตาราง
- **Verify (JSIT2016/JSP CONNX):** import → VAT 863+ รายการ; ภ.พ.30 ปี 2025 ตรงการรวมจาก DBF ทุกเดือนเป๊ะ (ม.ค. ขาย 208,764.08/14,613.50 n17, ซื้อ 175,573.75/12,290.14 n20; รวมขายทั้งปี 3,579,075.16 ≈ รายได้ใน P&L 3,579,157.45 = cross-check ผ่าน); รายละเอียด Jan ขาย 17 รายการ sum VAT ตรงรายงาน; UI ทั้ง 2 แท็บ + ชื่อบริษัทไทยถูกต้อง ผ่าน Preview ไม่มี console error
- **เครื่องมือ:** `tools/dbf_diag.py` (dump fields + sample จาก DBF ใด ๆ ด้วย cp874) — ใช้ reverse-engineer ISVAT/ISTAX และต่อยอด AR*/AP*/Payroll

### ✅ WHT / ภ.ง.ด.3 / 53 — เสร็จ (2026-06-05, โมดูล #11 + req v11 "PND.3/53")
นำเข้ารายการภาษีหัก ณ ที่จ่ายจาก Express **ISTAX.DBF** → รายงาน ภ.ง.ด.3/53 รายเดือน + รายละเอียดรายผู้ถูกหัก

- **Express ISTAX.DBF:** `TAXTYP` แยกแบบ **S03=ภ.ง.ด.3 (บุคคลธรรมดา)** / **S53=ภ.ง.ด.53 (นิติบุคคล)**, `TAXPRD`=เดือนภาษี, `AMOUNT`=ฐานเงินได้, `TAXRAT`=อัตรา%, `TAXAMT`=ภาษีหัก (ตรวจ AMOUNT×rate%=TAXAMT เป๊ะ), `TAXDES`=ประเภทเงินได้, `NAME/PRENAM/TAXID`=ผู้ถูกหัก, `TAXNUM/REFNUM/TAXDAT/TAXCOND/LATE`. รองรับชุดเงินได้ที่ 2 (AMOUNT2/...) → แตกเป็นรายการแยกเมื่อมีค่า
- **Domain:** `WhtEntry` (transactional, ดึงจาก Express 100%) + enum `WhtFormType` (Pnd3=3/Pnd53=53). migration `AddWhtEntries`. FormType เก็บเป็น string → ห้าม cast `(int)` ใน LINQ projection (materialize ก่อน เหมือน VAT)
- **Import:** `IExpressDbfAdapter.ReadWhtEntriesAsync` (ISTAX optional, เฉพาะ S03/S53) + `WhtEntryImporter.ImportAsync` (replace ทั้งชุดต่อบริษัท) เรียกใน `StartExpressImport` (audit `ImportWht`). ไม่มีปุ่ม import แยก
- **CQRS/API:** `GetWhtReportQuery` (รายเดือน แยก ภ.ง.ด.3/53 + รวมทั้งปี) / `GetWhtEntriesQuery` (ตัวกรองเดือน+แบบ) / `GetWhtYearsQuery`. `WhtController` — `GET /wht/report` · `/wht/years` · `/wht`
- **Frontend:** เมนู "หัก ณ ที่จ่าย" (กลุ่มภาษี) → `/wht` 2 แท็บ (ภ.ง.ด.3/53 รายเดือน header 2 ชั้น / รายละเอียดรายผู้ถูกหัก) + ปีจาก /wht/years + ExportMenu. **เปลี่ยน sidebar จาก `/tax-report?section=withholding` → `/wht`**
- **Verify (JSIT2016/JSP CONNX 2025):** import → WHT 46 รายการ (ภ.ง.ด.3: 1, ภ.ง.ด.53: 30 ในปี 2025); รายงาน ภ.ง.ด.3 ฐาน 12,000/ภาษี 360 n1, ภ.ง.ด.53 ฐาน 1,131,281.04/ภาษี 34,185.00 n30 — **ตรง ISTAX เป๊ะ** (cross-check python); รายละเอียด Jan ภ.ง.ด.53 5 ราย sum 4,696.16 ตรง; UI 2 แท็บ + ชื่อบริษัทไทยถูกต้อง ผ่าน Preview ไม่มี console error

#### ✅ ออกหนังสือรับรองหัก ณ ที่จ่าย (50 ทวิ) + ส่งอีเมล — เสร็จ (2026-06-05)
ต่อยอด WHT: ออกหนังสือรับรองการหักภาษี ณ ที่จ่าย (ฟอร์ม 50 ทวิ) เป็น PDF + ส่งให้ผู้ถูกหักทางอีเมล
- **มติ:** PDF ฝั่ง server (**QuestPDF** Community + ฟอนต์ไทยระบบ `Tahoma`, ตั้งได้ที่ `Wht:CertificateFont`) · ส่งเมล **SMTP** (`System.Net.Mail`, config `EmailSettings` กรอกเองตอน deploy) · จัดกลุ่ม **1 อีเมล/ผู้ถูกหัก** แนบ PDF ทุกใบ
- **อยู่รอด re-import:** อีเมลแยกเป็น `WhtPayee` (key บริษัท+TaxId, import ไม่ทับ); สถานะส่งเมลอยู่บน `WhtEntry` + เปลี่ยน importer เป็น **upsert ตาม `SourceKey`** (TAXNUM/REFNUM/ordinal#line) คงสถานะ — ไม่ลบทั้งชุดแล้ว
- **Domain:** WhtEntry เพิ่ม SourceKey/PayeeAddress/EmailStatus(enum NotSent/Sending/Sent/Failed)/EmailRecipient/EmailSentAt/EmailSentBy/EmailError + entity `WhtPayee`. migration `AddWhtDeliveryAndPayee` (ลบ WhtEntries เดิมที่ SourceKey ว่างก่อนสร้าง unique index)
- **บริการ:** `ThaiBahtText` (จำนวนเงิน→ตัวอักษรไทย), `IEmailSender`/`SmtpEmailSender`, `IWhtCertificatePdfService`/`WhtCertificatePdfService`, `WhtCertificateBuilder`
- **CQRS/API:** `GetWhtCertificatePdfQuery` (PDF preview) · `UpdateWhtPayeeEmailCommand` · `SendWhtCertificatesCommand` (group→gen→send→update status+audit). `WhtController` + `GET /wht/certificate` (application/pdf) · `PUT /wht/payee-email` · `POST /wht/send`
- **Frontend:** WhtEntriesTab + checkbox/เลือกทั้งหมด + คอลัมน์ อีเมล/สถานะส่งเมล(badge)/ส่งเมื่อ-โดย/error + toolbar (อีเมลผู้ถูกหัก/Preview/ส่งเมล) + `PayeeEmailModal` + `CertificatePreviewModal` (iframe blob)
- **Verify (ไม่ส่งเมลออกภายนอก):** PDF valid (%PDF, ฟิลด์ผู้หัก/ผู้ถูกหัก/จำนวน/ภาษี + ตัวอักษร "แปดร้อยสิบบาทถ้วน" ถูก); **req6** SMTP ไม่ตั้งค่า→Failed+error; **req5** ส่งผ่าน SMTP localhost catcher→Sent+SentAt/SentBy(admin)/Recipient (เมลถูกดักที่ localhost ไม่ออกนอกเครื่อง); UI ครบ — เลือกแถว/preview/ส่ง→badge อัปเดต, group ต่อผู้ถูกหัก (ริโก้ไม่มีอีเมล→ส่งไม่สำเร็จ, ACTIVEMEDIA→ส่งแล้ว) ผ่าน Preview ไม่มี console error. รีเซ็ตข้อมูลทดสอบ + คืน SMTP config ว่างแล้ว
- **ผู้ใช้ต้องตั้งค่าเอง:** `EmailSettings` (Host/Port/User/Password/FromAddress) ใน appsettings/env ก่อนใช้ส่งจริง; QuestPDF Community license ฟรีเมื่อรายได้ < $1M USD/ปี

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
