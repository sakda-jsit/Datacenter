# Implementation Status

สถานะการพัฒนา Phase 1 (อัปเดต: 2026-06-02)

## โมดูล (Phase 1)

| # | โมดูล | สถานะ | หมายเหตุ |
|---|---|---|---|
| 1 | Client Management | ✅ เสร็จ | full-stack |
| 2 | Import Data (Express DBF) | ✅ เสร็จ | ISINFO/GLACC/GLBAL/ISPRD + auto-post |
| 3 | VAT Management | ✅ เสร็จ (2026-06-05) | นำเข้า ISVAT.DBF + รายงาน ภ.พ.30 รายเดือน + รายละเอียดภาษีซื้อ/ขาย — ดูหัวข้อด้านล่าง |
| 4 | AR Management | ✅ เสร็จ (2026-06-05) | นำเข้า ARMAS/ARTRN → ลูกค้า + ใบแจ้งหนี้ + รายงานอายุหนี้ (aging) — ดูหัวข้อด้านล่าง |
| 5 | AP Management | ✅ เสร็จ (2026-06-05) | นำเข้า APMAS/APTRN → ผู้ขาย + ใบตั้งหนี้ + รายงานอายุหนี้เจ้าหนี้ — ดูหัวข้อด้านล่าง |
| 6 | Payroll | ⛔ รอสเปก DBF | ต้องการข้อมูลเงินเดือน + สูตรภาษี/ประกันสังคม |
| 7 | Bank / สมุดเงินฝาก | 🟡 สมุดเงินฝากเสร็จ | นำเข้า BKMAS/BKTRN → สมุดเงินฝาก + ยอดสะสม (ดูหัวข้อด้านล่าง); กระทบยอดกับ statement จริงยังรอไฟล์ธนาคาร |
| 8 | Trial Balance | ✅ เสร็จ | อ่านจาก Account + JournalEntry (ต้อง post ก่อน) |
| 9 | General Ledger | ✅ เสร็จ | running balance ต่อบัญชี |
| 10 | Financial Statement | ✅ เสร็จ | งบฐานะการเงิน + กำไรขาดทุน (ต้องตั้ง mapping บัญชี→บรรทัดงบ) |
| 11 | Tax Report (PP30/PND) | ✅ เสร็จ (2026-06-05) | **ภ.ง.ด.50** (X4 loop) + **ภ.พ.30** (ISVAT, โมดูล #3) + **ภ.ง.ด.3/53** (ISTAX) — ครบชุดแบบภาษีหลัก |
| 12 | Compliance Calendar | ✅ เสร็จ | |
| 13 | Closing Period | ✅ เสร็จ | งวด/วันสิ้นงวดจาก ISPRD, close/reopen/lock (reopen/lock เฉพาะ Admin) |
| 14 | Audit Log | ✅ เสร็จ | viewer + ตัวกรอง (paginated) |
| 15 | Dashboard & KPI | ✅ เสร็จ (operational) | นับลูกค้า/งาน compliance/import — ยังไม่รวม KPI การเงิน |

**เสร็จ 15/15 + ภ.ง.ด.50 + ภ.พ.30 + ภ.ง.ด.3/53** — ครบ Phase-1 ที่ทำได้จาก Express DBF. ที่เหลือ (Payroll, Bank Reconciliation) รอ DBF เงินเดือน / bank statement

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
| CAP (งบเปลี่ยนแปลงส่วนผู้ถือหุ้น) | ✅ เสร็จ (2026-06-05) | ไม่ | งบ CAP ต่อยอด FS engine (ทุนชำระแล้ว C1 + กำไรสะสม RE) ties to BS — ดูหัวข้อด้านล่าง |
| NOTE2 (หมายเหตุประกอบงบ) | ✅ เสร็จ (2026-06-05) | ไม่ | NoteTemplateSection (ข้อความแก้ได้/EffectiveYear) + NotesEngine (data binding ปีปัจจุบัน/ปีก่อน ดึงจาก TB) + แท็บ NOTE2 ในงบการเงิน — ดูหัวข้อด้านล่าง |
| DBD group-code taxonomy | ✅ เสร็จ (2026-06-08) | ไม่ | master `StatementLine` (global 29 บรรทัด) + validate RefCode ต้องมีใน master + หน้า "ผังมาตรฐาน (DBD)" + mapping dropdown ดึงจาก master (single source) — ดูหัวข้อด้านล่าง |
| Fixed Asset Register | ✅ เสร็จ (2026-06-05) | ไม่ (import ได้) | FixedAsset + AssetTypeMaster + DepreciationEngine (เส้นตรง 2 ชุด) + disposal กำไร/ขาดทุน auto + **import จาก Express FAMAS.DBF** + เมนู "สินทรัพย์ถาวร" `/fixed-assets` → generate adjustment เข้า TB — ดูหัวข้อด้านล่าง |
| Prepaid Schedule | ✅ เสร็จ (2026-06-06) | ไม่ | PrepaidExpense + ตัดจ่ายเส้นตรงตามวัน + เทียบ GL + generate adjustment; เมนู "ค่าใช้จ่ายจ่ายล่วงหน้า" `/prepaid` |
| Stock / FG↔TB | ✅ เสร็จ (2026-06-05) | ไม่ | นำเข้า STMAS + valuation + เทียบ GL + **data-as-of indicator** (ปิด open question ความสดของข้อมูล) — ดูหัวข้อด้านล่าง |
| Cash Count / Interest Income | ✅ เสร็จ (2026-06-07) | ไม่ | ตรวจนับเงินสด (ธนบัตร×จำนวน) + ดอกเบี้ยรับเงินให้กู้ (day-count + SBT/ท้องถิ่น) → generate adjustment เข้า TB; เมนู `/cash-count`, `/interest-income` — ดูหัวข้อด้านล่าง |
| AR (ลูกหนี้) จาก ARMAS/ARTRN | ✅ เสร็จ (2026-06-05) | ไม่ | ลูกค้า + ใบแจ้งหนี้ + aging — ดูหัวข้อด้านล่าง |
| AP (เจ้าหนี้) จาก APMAS/APTRN | ✅ เสร็จ (2026-06-05) | ไม่ | ผู้ขาย + ใบตั้งหนี้ + aging — ดูหัวข้อด้านล่าง |
| Bank / สมุดเงินฝาก จาก BKMAS/BKTRN | ✅ เสร็จ (2026-06-08) | ไม่ | สมุดเงินฝาก + **กระทบยอด statement เต็มรูป** (RPT-015/016): อัปโหลด PDF (SCB/KBANK/TTB parse 100%) / Excel/CSV → จับคู่ BKTRN → matched/unmatched + generate adjustment — ดูหัวข้อด้านล่าง |
| Subsequent Payment Check | ✅ เสร็จ (2026-06-08) | ไม่ | RPT-019: ตรวจค้างจ่ายปีปิดงบ จ่ายจริงปีถัดไปหรือยัง — อ่าน GLJNLIT ปีถัดไป**สด**จาก Express (docs/17) — ดูหัวข้อด้านล่าง |
| TAX engine (เต็มรูป) | ✅ เสร็จ (2026-06-14) | ไม่ | คำนวณภาษีเงินได้นิติบุคคลจากกำไรบัญชี + บวกกลับ/หักออก + ขาดทุนสะสม + อัตรา SME ขั้นบันได → mirror X4 ลงงบ — ดูหัวข้อด้านล่าง |
| PP30 auto จาก VAT | ✅ เสร็จ (2026-06-05) | ไม่ (ISVAT) | นำเข้า ISVAT.DBF → รายงาน ภ.พ.30 รายเดือน — ดูหัวข้อด้านล่าง |
| PND.3/53 จาก ISTAX | ✅ เสร็จ (2026-06-05) | ไม่ (ISTAX) | นำเข้า ISTAX.DBF → รายงาน ภ.ง.ด.3/53 รายเดือน + รายละเอียด — ดูหัวข้อด้านล่าง (ไม่ต้องพึ่ง PDF) |
| Field-level audit ทุก field | 🟡 มี Audit Log พื้นฐาน | ไม่ | ยังไม่ field-level (docs/18) |
| Attachment / Evidence | ❌ ยังไม่เริ่ม | ไม่ | docs/18 |
| Audit log export | ✅ เสร็จ (2026-06-07) | ไม่ | export **ทั้งชุดตามตัวกรอง** (Excel/CSV/PDF) ผ่าน /audit-log/export (cap 50k) + ตัวกรอง Action/EntityName (/filter-options) + แสดงจำนวนรวม |
| Report package (draft/review/final/lock + version) | ✅ เสร็จ (2026-06-05) | ไม่ | ReportPackage + workflow + snapshot ชื่อบริษัท/ยอดงบ ตอน finalize — ดูหัวข้อด้านล่าง |
| Snapshot อัตลักษณ์บริษัทตอนล็อกงบ | ✅ เสร็จ (ใน Report Package) | ไม่ | snapshot LegalName/TaxId/Branch/Address + ยอดงบต่อ version (แก้ deferred จากงาน LegalName) |
| Snapshot Express DBF เก็บถาวร 10 ปี | ✅ เสร็จ (2026-06-07) | ไม่ | ImportSnapshot/ImportSnapshotFile + zip ไฟล์ DBF ต้นฉบับ + checksum/row count + RetainUntil; ปุ่ม "หลักฐาน" ในหน้านำเข้า — ดูหัวข้อด้านล่าง |

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
- **Frontend:** เมนู "ภาษีมูลค่าเพิ่ม" → `/vat` 3 แท็บ (ภ.พ.30 รายเดือน + **ใบกรอก ภ.พ.30 (e-Filing)** + รายละเอียดภาษีซื้อ/ขาย) + ปีจาก /vat/years (auto เลือกปีล่าสุด) + ExportMenu ทั้งสองตาราง
- **ใบกรอก ภ.พ.30 (e-Filing) — เสร็จ (2026-06-14):** ใบช่วยกรอกค่าลงหน้า "ข้อมูลการคำนวณภาษี" ของเว็บ RD (ดู reference/vat/pp30_input.jpg) — เลือกเดือน → แสดงค่าตรงช่อง/ลำดับเดียวกับฟอร์มยื่นจริง: ยอดขายในเดือนนี้ (=outputBase+outputZeroRated) / ยอดขายอัตรา0 / ยอดขายยกเว้น (0 — ISVAT ไม่เก็บ) / ยอดซื้อมีสิทธิ์ (inputBase) / ภาษีขาย (outputVat) / ภาษีซื้อ (inputVat) / ภาษีเดือนนี้รอคำนวณ (=ขาย−ซื้อ) / ภาษีชำระเกินยกมา (กรอกเอง — ระบบไม่เก็บยอดสะสม) / ภาษีสุทธิรอคำนวณ. **frontend-only** (reuse useVatReport ไม่แตะ backend) + ExportMenu (พิมพ์/บันทึก). **หมายเหตุ:** RD รองรับอัปโหลดไฟล์โอนย้าย (.csv/.txt + map คอลัมน์ที่ step2 + เลือก delimiter/header เอง, ดู reference/vat/pp30_txt.jpg) — เฟสนี้ทำใบช่วยกรอกมือก่อน (header/สาขา/เดือน อยู่บนฟอร์มเว็บ ไม่อยู่ในไฟล์); export ไฟล์โอนย้ายอัตโนมัติทำต่อได้
- **Verify ใบกรอก (JSP CONNX 226, ม.ค.2026):** ทุกช่องตรง API VatReport เป๊ะ — ยอดขาย 78,584.20 / ภาษีขาย 5,500.88 / ยอดซื้อ 56,995.85 / ภาษีซื้อ 3,989.71 / ภาษีเดือนนี้ 1,511.17; กรอกชำระเกินยกมา 500 → สุทธิ 1,011.17 (recompute สด); UI ผ่าน Preview ไม่มี console error
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

##### ✅ แก้ preview จอดำ + เลือกรูปแบบส่งเมล (2026-06-05)
- **Preview จอดำ (แก้แล้ว):** เดิมใช้ `<iframe src=blob:pdf>` — บางเบราว์เซอร์เรนเดอร์ PDF เป็นจอดำ (PDF เองไม่ดำ ยืนยันด้วย PyMuPDF: 95% ขาว). แก้: เพิ่ม `IWhtCertificatePdfService.GenerateImages` (QuestPDF `GenerateImages` PNG 150dpi) + `GetWhtCertificateImagesQuery` + `GET /wht/certificate/images` (คืน data URL `image/png`). `CertificatePreviewModal` แสดง `<img>` แทน iframe (เรนเดอร์ได้ทุกเบราว์เซอร์) + ปุ่มดาวน์โหลด PDF ยังอยู่
- **รูปแบบส่งเมล (เลือกได้):** enum `WhtSendGrouping` { ByPayee=0, Single=1 }; `SendWhtCertificatesCommand` เพิ่ม `Grouping`+`RecipientEmail`. **ByPayee** = 1 อีเมล/ผู้ถูกหัก (เดิม) · **Single** = รวมทุกฉบับเป็น 1 อีเมล ส่งไปอีเมลเดียวที่ระบุ (validate ต้องมี RecipientEmail). `WhtSendModal` (radio 2 แบบ + ช่องอีเมลเมื่อเลือก Single)
- **Verify:** image endpoint คืน PNG valid (98KB, header 89504e47); preview แสดงรูป 50 ทวิ ไม่ดำ; send Single ไม่มีอีเมล→422, Single+อีเมล→1 group (recipient ที่ระบุ, 2 ฉบับ), ByPayee→2 groups ตามผู้ถูกหัก (success=false เพราะ SMTP ไม่ตั้งค่า = ไม่มีเมลออกจริง)

##### ✅ ฟอร์ม 50 ทวิ ตรงแบบราชการ + อีเมล save DB + ค่าเริ่มต้นการส่ง (2026-06-05)
- **ฟอร์มตรงแบบ ภ.ง.ด.3/53 (ไฟล์แนบราชการ):** rewrite `WhtCertificatePdfService.Compose` — หัว "ลำดับที่ *__ ในแบบยื่น {form} เลขที่ {DocumentNo}" + ฉบับที่ 1, กล่องผู้มีหน้าที่หัก/ผู้ถูกหัก (มีกรอบ + ☑สำนักงานใหญ่/☐สาขา), **ตารางประเภทเงินได้ครบ 1–6** (รวมหัวข้อย่อย 40(4)(ก)/(ข) + มาตรา 3 เตรส + อื่นๆ), แถวรวม, ตัวอักษร, **ผู้จ่ายเงิน checkbox** (☑/☐ หักภาษี ณ ที่จ่าย/ออกตลอดไป/ครั้งเดียว/อื่นๆ ตาม ConditionType), ลงชื่อ "ผู้มีหน้าที่หักภาษี ณ ที่จ่าย" + ประทับตรานิติบุคคล + วันที่, **หมายเหตุ + คำเตือน (ม.35)**
- **จัดหมวดเงินได้:** `WhtCertificateBuilder.ClassifyIncome` (keyword) → วางจำนวนเงินในแถวที่ตรง (เงินเดือน→1, ค่าธรรมเนียม/นายหน้า/วิชาชีพ→2, ลิขสิทธิ์→3, ดอกเบี้ย→40(4)(ก), ปันผล→40(4)(ข), **ค่าจ้างทำของ/ขนส่ง/โฆษณา/ประกันวินาศภัย/รางวัล-ส่งเสริมการขาย→5 มาตรา 3 เตรส**, **ค่าบริการ/ค่าเช่า/อื่นๆ→6** ใส่ชื่อในช่องระบุ — ตรงการจัดของ Express ตามไฟล์แนบ). model เพิ่ม `IncomeCategory`+`ConditionType`. ตาราง 40(4)(ข) แตกข้อย่อยครบ (1)(1.1–1.4)(2)(2.1–2.5). **verify:** PND3 ค่าจ้างทำของ→หมวด 5 (6,000/180); PND53 ค่าเช่าฯ→หมวด 6 (2,087.24/104.36); ข้อ 4–6 ตรงไฟล์แนบ
- **อีเมลผู้ถูกหัก save DB:** `UpdateWhtPayeeEmailCommand` upsert `WhtPayees` (บริษัท+TaxId) + SaveChanges → **บันทึกถาวร ไม่ต้องป้อนใหม่** และ re-import ไม่ทับ (ตอบคำถามผู้ใช้: เก็บใน DB แล้ว)
- **ค่าเริ่มต้นการส่ง (save default):** `WhtSendModal` เพิ่ม checkbox "บันทึกเป็นค่าเริ่มต้นของบริษัทนี้" → เก็บ {mode,email} ใน `localStorage[wht.sendDefault.{companyId}]`, โหลด pre-select ตอนเปิด modal ครั้งถัดไป. **verify:** เลือก Single+อีเมล+save → localStorage เก็บ `{"mode":1,"email":...}` ครบ

##### ✅ ลายเซ็นผู้มีหน้าที่หัก + refine layout 50 ทวิ ตามแบบราชการ (2026-06-06, commit d0443cf + fix)
- **ลายเซ็น:** `ClientCompany.SignatureImage` (varbinary) + migration `AddClientCompanySignature`; endpoints `GET/POST(multipart ≤2MB)/DELETE /wht/signature` (`UpdateClientCompanySignatureCommand`/`GetClientCompanySignatureQuery`); UI ปุ่ม "ลายเซ็น" → `SignatureModal` (preview/upload/ลบ). render เหนือเส้น "ลงชื่อ" (เยื้องซ้าย). **auto-trim** ขอบโปร่งใส(α≤24)/พื้นขาว(RGB≥240) ตอน upload ด้วย **SixLabors.ImageSharp 2.1.3** (`ISignatureImageProcessor`/`SignatureImageProcessor`) — เช่น 600×400→178×105
- **refine layout (ตามผู้ใช้ทบทวนกับไฟล์แนบ):** checkbox 4(ข) เหลือเฉพาะ (1.1)–(1.4); ตารางใช้เส้นแบ่งคอลัมน์แนวตั้งอย่างเดียว (ไม่มีเส้นแนวนอน); header ลำดับที่(ซ้าย)/เลขที่(ขวา) บรรทัดเดียว + "ในแบบ แบบยื่น {form}" + ฉบับที่1 กึ่งกลางตัวหนา; กล่องคู่สัญญา title(ซ้าย)/เลขผู้เสียภาษี(ขวา) + ชื่อ(ซ้าย)/สำนักงานใหญ่(ขวา) เป็นข้อความ (ไม่มี checkbox สาขา); คอลัมน์ ratio 19:2.4:3.3:3.0; ข้อ 5 (เตรส) ประเภทที่ระบุขึ้นบรรทัดใหม่ + ข้อมูล(วันที่/เงิน/ภาษี)ชิดล่างตรงบรรทัด 2
- **bugfix (2026-06-06):** ลายเซ็นกว้างมาก (เช่น 1400×120) ทำ `FitHeight()` ขยายล้นเซลล์ → `DocumentLayoutException` (PDF 500). แก้เป็น **`FitArea()`** (พอดีทั้งกว้าง+สูง) — verify: wide 1400×120 render ผ่าน, normal ไม่เปลี่ยน *(แก้แล้วรอ commit)*

### ✅ AR / ลูกหนี้การค้า — เสร็จ (2026-06-05, โมดูล #4)
นำเข้าลูกค้า + ใบแจ้งหนี้ลูกหนี้จาก Express → รายงานอายุหนี้ (aging) + ใบแจ้งหนี้ + รายชื่อลูกค้า

- **Express:** `ARMAS.DBF` (ลูกค้า: CUSCOD/CUSNAM/TAXID/ที่อยู่/PAYTRM/ACCNUM/STATUS; อีเมลแฝงใน REMARK "E-MAIL:..." → regex แยก) + `ARTRN.DBF` (ธุรกรรม: **RECTYP='3'=ใบแจ้งหนี้ IV**, '9'=ใบเสร็จ RE ข้าม). ยอดค้าง = `REMAMT`, รับชำระ = `RCVAMT`, ปิดแล้ว = `CMPLAPP='Y'`
- **Domain:** `Customer` (master, upsert by รหัส) + `ArInvoice` (transactional, replace ต่อบริษัท, denormalize CustomerName). migration `AddArCustomersAndInvoices`
- **Import:** `ReadCustomersAsync`/`ReadArInvoicesAsync` + `ArImporter` (upsert ลูกค้า + replace ใบแจ้งหนี้) เรียกใน StartExpressImport (audit `ImportAr`)
- **CQRS/API:** `GetCustomers` (+ยอดค้างรวม/จำนวนใบค้างต่อราย) · `GetArInvoices` (filter ปี/ค้างชำระ/ลูกค้า) · `GetArAging` (aging ณ วันที่: ยังไม่ถึงกำหนด/1-30/31-60/61-90/>90 จาก DueDate) · `GetArYears`. `ArController` `/ar/customers,/invoices,/aging,/years`
- **Frontend:** เมนู "ลูกหนี้" `/ar` 3 แท็บ (อายุหนี้ + date picker / ใบแจ้งหนี้ + filter ค้างชำระ / ลูกค้า) + ExportMenu ทุกตาราง
- **Verify (JSP CONNX, asOf 2026-06-05):** import → 54 ลูกค้า (อีเมล parse 8 ราย), 272 ใบแจ้งหนี้; **aging รวม 178,289.19 ตรงยอดค้างใน ARTRN (REMAMT) เป๊ะ** — 3 ลูกค้าค้าง (เอเซีย พรีซิชั่น 109,274.19, ไบแอ็คเตอร์ 64,200, บูรพา 4,815), bucketing ตาม DueDate ถูกต้อง; UI 3 แท็บ + ชื่อลูกค้าไทย ผ่าน Preview ไม่มี console error

### ✅ AP / เจ้าหนี้การค้า — เสร็จ (2026-06-05, โมดูล #5 — กระจกของ AR)
นำเข้าผู้ขาย + ใบตั้งหนี้เจ้าหนี้จาก Express → รายงานอายุหนี้เจ้าหนี้ + ใบตั้งหนี้ + รายชื่อผู้ขาย (โครงเดียวกับ AR เป๊ะ)

- **Express:** `APMAS.DBF` (ผู้ขาย: SUPCOD/SUPNAM/TAXID/ที่อยู่/PAYTRM/ACCNUM; STATUS ผสม '2'/'A'/'' → IsActive = STATUS≠'0') + `APTRN.DBF` (**RECTYP='3'=ใบตั้งหนี้ซื้อ RR**; '9'=จ่าย PS, '7'=OE, '1'=HP ข้าม). ยอดค้าง = `REMAMT`, จ่ายแล้ว = `PAYAMT`, ปิด = `CMPLAPP='Y'`
- **Domain:** `Supplier` (master, upsert) + `ApInvoice` (replace ต่อบริษัท). migration `AddApSuppliersAndInvoices`
- **Import/CQRS/API:** `ApImporter` ใน StartExpressImport (audit `ImportAp`); `GetSuppliers`/`GetApInvoices`/`GetApAging`/`GetApYears`; `ApController` `/ap/suppliers,/invoices,/aging,/years`
- **Frontend:** เมนู "เจ้าหนี้" `/ap` 3 แท็บ (อายุหนี้/ใบตั้งหนี้/ผู้ขาย) + ExportMenu
- **Verify (JSP CONNX, asOf 2026-06-05):** 345 ผู้ขาย, 67 ใบตั้งหนี้; **aging รวม 39,457.51 ตรง REMAMT เป๊ะ** (5 ผู้ขายค้าง: คอปเปอร์ไวร์ด 20,698.32, เอสเอชดี 7,453.30, HOME PRODUCTS 6,493.89, เอ็น.ที. 2,882.01, ออฟฟิศเมท 1,929.99); UI 3 แท็บ + ชื่อผู้ขายไทย ผ่าน Preview ไม่มี console error

### ✅ Stock / สินค้าคงคลัง — เสร็จ (2026-06-05, req v11 docs/15)
> **OPEN QUESTION (เคลียร์แล้ว 2026-06-05):** stock เป็น snapshot ณ ตอน import ไม่ real-time → เพิ่ม **data-as-of indicator** ("ข้อมูลสินค้าคงคลังเป็น snapshot — นำเข้าล่าสุด {วันเวลา}") + ปุ่ม "นำเข้าข้อมูลใหม่" บนหน้า valuation (ไม่เปลี่ยนไปอ่าน DBF สด เพราะผิดหลักปิดงบ). `DataAsOf` = max(StockItem.CreatedAt). **✅ ขยายไปทุกโมดูลแล้ว (2026-06-06):** shared component `DataAsOfBanner` (frontend) + `DataAsOf` = max(entity.CreatedAt) ใน DTO ของ VAT (VatReport) / WHT (WhtReport) / AR (ArAgingReport) / AP (ApAgingReport) / Bank (BankBook) / **FA (FixedAssetListDto — wrap list เดิม `IReadOnlyList` → `{Items, DataAsOf}`)** — Stock refactor มาใช้ component กลางตัวเดียวกัน. verify: API ทุกตัวคืน dataAsOf + banner แสดงจริงบน VAT/WHT/AR/FA (UTC→local +543 ปี พ.ศ.).

นำเข้ายอดสินค้าคงเหลือจาก Express → รายงานมูลค่า + เทียบบัญชีสินค้าคงเหลือใน GL (FG↔TB, ผลต่างให้ปรับปรุงเอง ไม่ auto ตามมติ #5)

- **Express STMAS.DBF:** `STKCOD`(รหัส) `STKDES`(ชื่อ) `STKTYP`(ประเภท) `STKGRP`(กลุ่ม) `ACCCOD`(หมวด) `QUCOD`(หน่วย) **`TOTBAL`(จำนวนคงเหลือ)** `UNITPR`(ต้นทุน/หน่วย) **`TOTVAL`(มูลค่า)** `STATUS`. เก็บเฉพาะรายการที่มียอด/มูลค่า≠0 (snapshot ปัจจุบัน; ตัด master ยอด 0 ออก)
- **Domain:** `StockItem` (replace ต่อบริษัท). migration `AddStockItems`. `StockImporter` ใน StartExpressImport (audit `ImportStock`)
- **CQRS/API:** `GetStockItems` (รายการ) · `GetStockValuation` (มูลค่า + สรุปกลุ่ม + เทียบ GL): หาบัญชีสินค้าคงเหลือจากชื่อ ("สินค้าคงเหลือ"/"Inventory") → ยอดสะสมถึงสิ้นปีงบ (สินทรัพย์ debit−credit) เทียบ TOTVAL → ผลต่าง. `StockController` `/stock`,`/stock/valuation`
- **Frontend:** เมนู "สินค้าคงคลัง" (กลุ่มบัญชี) → `/stock` 2 แท็บ (มูลค่า/เทียบ GL + KPI 3 ใบ + สรุปกลุ่ม + เทียบ GL / รายการสินค้า) + ExportMenu
- **Verify (JSP CONNX):** 19 รายการมียอด, **มูลค่ารวม 11,813.09 ตรง STMAS เป๊ะ**; กลุ่ม FG; ผังบัญชีเป็นอังกฤษ (บริษัทบริการ ไม่มีบัญชีสินค้าคงเหลือใน GL) → GL=0, ผลต่าง 11,813.09 (เคสจริง: สินค้ามีแต่ไม่มีบัญชี → ต้องปรับปรุง); UI 2 แท็บ + คงเหลือติดลบสีแดง ผ่าน Preview ไม่มี console error
- **ขอบเขต v1:** valuation snapshot จาก TOTVAL ของ Express (ยังไม่ทำ FIFO costing เอง — Express คำนวณมูลค่าให้แล้ว); ผลต่าง FG↔TB แสดงให้บันทึก adjustment เอง

### 🟡 Bank / สมุดเงินฝากธนาคาร — เสร็จ (2026-06-05; โมดูล #7 ส่วนสมุดเงินฝาก)
นำเข้าบัญชีธนาคาร + รายการเดินบัญชีจาก Express → สมุดเงินฝาก (running balance). กระทบยอดกับ statement จริงยังรอไฟล์ธนาคารภายนอก

- **Express:** `BKMAS.DBF` (บัญชี: BNKACC/BNKNAM/BRANCH/BNKNUM/ACCNUM(GL)/BALFWD) + `BKTRN.DBF` (เดินบัญชี). **ทิศทาง: `JNLTRNTYP`='0'=เงินเข้า / '1'=เงินออก** (ยืนยันจาก sample: bP จ่ายเช็ค/BW ถอน/BT โอนออก=ออก; BD ฝาก/Bx โอนเข้า/bR รับ=เข้า)
- **Domain:** `BankAccount` (master upsert) + `BankTransaction` (replace, IsDeposit). migration `AddBankAccountsAndTransactions`. `BankImporter` ใน StartExpressImport (audit `ImportBank`)
- **CQRS/API:** `GetBankAccounts` (+ยอดคงเหลือปัจจุบัน = BALFWD+เคลื่อนไหวสุทธิ) · `GetBankBook` (สมุดเงินฝากต่อบัญชี/ปี: opening=BALFWD+net ก่อนปี, running balance ต่อรายการ, ฝาก/ถอน/คงเหลือ) · `GetBankYears`. `BankController` `/api/v1/bank/accounts,/book,/years`
- **Frontend:** เมนู "ธนาคาร / สมุดเงินฝาก" `/bank-reconciliation` (เปลี่ยนจาก ComingSoon) 2 แท็บ (สมุดเงินฝาก + เลือกบัญชี/ปี + ยอดยกมา+running / บัญชีธนาคาร) + ExportMenu
- **Verify (JSP CONNX):** 4 บัญชี, KASIKORN/SA R (acc 11) 331 รายการ; สมุดปี 2025 = 249 รายการ running balance สะสมถูกต้อง (ฝาก 403,455.37 ถอน 3,342,684.34); BALFWD ใน Express=0 → opening 0 (ยอดคงเหลือเป็นยอดเคลื่อนไหวสะสม); เงินเข้า/ออกแยกถูกตาม JNLTRNTYP; UI 2 แท็บ ผ่าน Preview ไม่มี console error
- **ขอบเขต:** เป็น "สมุดเงินฝาก/cash book" จากฝั่งสมุดบัญชี (Express) — **ยังไม่ใช่ bank reconciliation เต็มรูป** (ต้องมีไฟล์ statement จากธนาคารมาจับคู่ matched/unmatched ซึ่งไม่อยู่ใน Express)

### ✅ CAP / งบแสดงการเปลี่ยนแปลงส่วนของผู้ถือหุ้น — เสร็จ (2026-06-05, req v11)
ต่อยอด FinancialStatement engine — ไม่มี entity/migration ใหม่ (คำนวณจาก GL + StatementLines Section 'E')

- **องค์ประกอบ:** StatementLines Section 'E' = **C1 (ทุนที่ออกและชำระแล้ว)** + **RE (กำไรสะสม)**
- **Query `GetEquityChanges`:** ต่อองค์ประกอบ → opening (สะสม net ถึงสิ้นปีก่อน, flip), closing (ถึงสิ้นปีนี้; RE ใช้สูตรเดียวกับงบดุล = flip closing + netProfit), netProfit (จาก BuildProfitLoss ฐานเดียวกับ BS), otherChange = closing−opening−netProfit (เพิ่มทุน/เงินปันผล/ปรับปรุง catch-all). `FinancialStatementController` `GET /financial-statement/equity-changes`
- **ออกแบบให้ ties to BS เสมอ:** closing รวม = ส่วนผู้ถือหุ้นในงบดุล (ใช้สูตร RE เดียวกัน) → มี flag `tiesToBalanceSheet`
- **Frontend:** แท็บใหม่ "งบส่วนผู้ถือหุ้น" ในหน้างบการเงิน — matrix (คอลัมน์=องค์ประกอบ+รวม, แถว=ยอดต้นปี/กำไรสุทธิ/เปลี่ยนแปลงอื่น/ยอดปลายปี) + ExportMenu
- **Verify (JSP CONNX 2025):** closing รวม **−704,161.20 = BS totalEquity เป๊ะ (ties=true)**; ทุน 1M→2M (เพิ่มทุน), RE opening −1,583,494.46 + กำไร 519,308.86 + อื่น −1,639,975.60 = −2,704,161.20; UI ผ่าน Preview ไม่มี console error
- **หมายเหตุ:** "เปลี่ยนแปลงอื่น" เป็น catch-all (เพิ่มทุน+เงินปันผล+ปรับปรุง RE จาก closing entries ของ Express) — แยกเงินปันผล/เพิ่มทุนชัดเจนต้องมี refcode เฉพาะ (อนาคต)

### ✅ NOTE2 / หมายเหตุประกอบงบการเงิน — เสร็จ (2026-06-05, req v11 docs/13 ข้อ 5)
แยก 2 ส่วนชัดเจนตาม requirement: **(1) ข้อความ template แก้ได้ (EffectiveYear)** ↔ **(2) data binding ตัวเลขดึงจาก TB อัตโนมัติ ห้ามแก้**

- **Domain:** `NoteTemplateSection` (ClientCompanyId nullable = template กลาง/null vs เฉพาะบริษัท, EffectiveYear **ค.ศ.**, NoteKey "1".."7"/"6.5"/"6.11"/"6.12", Title, BodyText nvarchar(max), SortOrder) + migration `AddNoteTemplateSections` (unique (ClientCompanyId,EffectiveYear,NoteKey)). seed `NoteTemplateSeed` = ข้อความ TFRS-NPAEs (ปรับปรุง 2566) มาตรฐาน EffectiveYear **2024** (=พ.ศ.2567) เป็น template กลาง
- **placeholder แทนค่าตอน render:** `{{CompanyName}} {{TaxId}} {{Address}} {{FiscalYear}} {{FiscalYearTh}} {{PriorYear}} {{PriorYearTh}}` จาก ClientCompany profile
- **NotesEngine (pure):** สร้างตาราง breakdown ตาม RefCode (1:1 กับ StatementLines) ปีปัจจุบัน/ปีก่อน + sign convention เดียวกับ FS engine. catalog: 6.1=A1, 6.2=A7/A8, 6.3=A3, 6.4=A4/TXR, 6.8=A6, 6.9=L1/L2/TXP, 6.10=L4/L5, 6.14=X1, 6.15=X2
- **ฐานยอด = สะสมถึงสิ้นปี (cumulative) ทั้ง BS และ PL** — ตรงกับหลักของ `GetProfitLossQueryHandler` (Express ทับยอด P&L ที่ปลายปี + opening ล้างยอดปีก่อน) เพื่อให้ **ทุกหมายเหตุลงตรงกับงบที่แสดง 100%**
- **6.6/6.7 (ที่ดิน-อาคาร-อุปกรณ์/สินทรัพย์ไม่มีตัวตน):** ตารางการเคลื่อนไหว (ราคาทุน + ค่าเสื่อมสะสม: ต้นปี/เพิ่ม/ลด/ปลายปี) จาก FA register ผ่าน `DepreciationEngine.AsOf` — แยกสินทรัพย์ไม่มีตัวตนด้วยชื่อประเภท (โปรแกรม/ซอฟต์แวร์/ลิขสิทธิ์). ใช้ `bookCur.OpeningAccumulated` เป็นยอดต้นปี (ไม่ใช่ AsOf(ปีก่อน).Closing ซึ่งคืน 0 เมื่อยกยอดมาปีปัจจุบัน) → ต้น+เพิ่ม−ลด = ปลาย **ลงตรงพอดี**
- **6.13 (ต้นทุนขาย):** องค์ประกอบจากบัญชีกลุ่ม "C" ยอดสะสม → ยอดรวม = ผลรวม = บรรทัดต้นทุนขายในงบ PL; สินค้าคงเหลือต้นงวด/ปลายงวดแสดงเป็น memo (ผังบัญชีลูกค้าบันทึกต้นทุนขายแบบสุทธิ ไม่ใช่ยอดซื้อ → ไม่บวกต้น/หักปลายซ้ำ)
- **CQRS/API:** `GetNotesToFsQuery` (เอกสารเต็ม) / `GetNoteTemplateSectionsQuery` (ข้อความที่มีผล สำหรับแก้ไข) / `UpsertNoteTemplateSectionCommand` (บันทึก override เฉพาะบริษัท ไม่แตะ template กลาง) / `ResetNoteTemplateSectionCommand` (ลบ override กลับมาตรฐาน). `FinancialStatementController`: `GET /notes`, `GET /note-templates`, `PUT /note-templates`, `POST /note-templates/reset`
- **เลือก template ตอน render:** ต่อ NoteKey → บริษัท override ก่อน default กลาง, EffectiveYear มากสุดที่ ≤ ปีงบ
- **Frontend:** แท็บ "หมายเหตุประกอบงบ (NOTE2)" ในหน้างบการเงิน — render เอกสารเต็มเรียงตาม SortOrder (narrative + schedule + movement + cost-of-sales) คอลัมน์ปี พ.ศ. (2568/2567) + ปุ่ม "แก้ไขข้อความ" (modal แก้ title/body ต่อข้อ + คืนค่ามาตรฐาน) + ExportMenu generic (Excel/CSV/PDF)
- **Export Excel รูปแบบงบ (เหมือน sheet NOTE2 ใน 2025_JSPC_FIN.xlsx):** สร้าง**ฝั่ง server ด้วย ClosedXML** (`INote2ExcelExporter` + `Note2ExcelExporter`) เพราะ SheetJS/xlsx-js-style เขียน **page break ไม่ได้**. `GET /financial-statement/notes/excel` → `GetNotesExcelQuery` (ดึง DTO ผ่าน IMediator → exporter). ตรง reference: ฟอนต์ **AngsanaUPC** (หัว 18 bold/เนื้อ 16), หัวเอกสาร (ชื่อบริษัท/หมายเหตุ/งวด) + ลงชื่อกรรมการ **ทุกหน้า**, คอลัมน์ A=เลขข้อ B=label, ปีปัจจุบัน=**F** ปีก่อน=**H**, เลขแบบบัญชี `_-* #,##0.00_-;...`, ตาราง movement 6.6/6.7 มี**กรอบ** (ยอดต้นปี D/เพิ่ม E/ลด F/ยอดปลายปี H), แถวรวม bold เส้นบน+ล่างคู่, **แบ่งหน้า A4 ตามกลุ่มหมายเหตุ** (AddHorizontalPageBreak), ตารางอัตราค่าเสื่อมในข้อ 3, รับ `directorName` (param). **verify:** openpyxl อ่านไฟล์จริง — AngsanaUPC/merge A1:H1/page breaks 10 จุด/accounting numFmt/กรอบ movement/ยอดตรง (3,124,644.43). ปุ่ม "ส่งออก Excel (รูปแบบงบ)" ดึง blob จาก backend
- **ข้อจำกัด/อนาคต:** แบ่งหน้าเป็นแบบ "ตามกลุ่มหมายเหตุ" (ไม่ fix 33 แถว/หน้าเป๊ะเท่า reference แต่ขึ้นหน้าใหม่จุดเดียวกัน); ชื่อกรรมการรับเป็น param (ระบบไม่เก็บ); อัตราค่าเสื่อมในข้อ 3 = ค่ามาตรฐาน hardcode
- **Verify (JSP CONNX 2025):** **ทุก note ลงตรงงบ** — 6.1=A1 1,394,994.10, 6.3=A3, 6.9=L1, 6.10=L4 107,950.75 (ตรงงบดุล); 6.13=PL C 3,159,763.18, 6.14=PL X1 113,419.48, 6.15=PL X2 1,209,826.33 (ตรง PL); 6.6 cost close 3,124,644.43 + accum open 2,571,733.30 + net open 338,860.71 (**ตรง workbook Excel เป๊ะ**, ปลายปีต่าง ≤6 บาท จาก rounding ค่าเสื่อม), ตาราง movement foots; 9 narratives แทน placeholder ครบ; แก้ไข/บันทึก override (isCompanyOverride=true) + reset ทำงานผ่าน Preview ไม่มี console error
- **หมายเหตุ/อนาคต:** ป้ายชื่อหมวดใน 6.6 ที่ยังไม่ map AssetType จะโชว์ CategoryCode (CA/CAO); ตาราง "อัตราค่าเสื่อม" ในข้อ 3 ฝังเป็นข้อความ (ยังไม่ทำเป็น sub-table แยก); OLE_LINK header เป็น page header ตาม docs/13

### ✅ Report Package / ชุดรายงานงบ (version + lock) — เสร็จ (2026-06-05, req v11 #9 + docs/18)
จัดเวอร์ชันงบการเงินต่อ (บริษัท, ปีงบ) + เวิร์กโฟลว์ล็อกงบที่ยื่น + snapshot อัตลักษณ์บริษัท/ยอดงบ — **ปิดงานที่เลื่อนมาจาก LegalName (snapshot ตอนล็อกงบ)**

- **Domain:** `ReportPackage` (ClientCompanyId, FiscalYear, Version, Status enum, Title, Note, Snapshot: CompanyName/TaxId/BranchCode/Address, ยอด: TotalAssets/Liabilities/Equity/Revenue/NetProfit, FinalizedAt/By, LockedAt/By) + enum `ReportPackageStatus` (Draft/Review/Final/Locked). migration `AddReportPackages`. unique (Company, Year, Version)
- **เวิร์กโฟลว์:** Draft → Review → **Final (capture snapshot: ชื่อบริษัท LegalName + ยอดงบจาก BS/PL ผ่าน IMediator)** → Locked (ยื่นแล้ว ห้ามแก้). Locked → Final = ปลดล็อก (คง snapshot, audit). ยื่นเพิ่มเติม = สร้าง version ใหม่ (เลขถัดไป, version เดิม freeze). ลบได้เฉพาะ Draft
- **CQRS/API:** Create (auto version) / SetStatus (กฎ transition + snapshot/lock/unlock + audit) / Delete (Draft เท่านั้น) / GetReportPackages. `ReportPackagesController` `/api/v1/report-packages`
- **Frontend:** เมนู "ชุดรายงานงบ" `/report-packages` (กลุ่มรายงานและปิดงวด) — ตาราง version/สถานะ(badge)/ยอด snapshot/ผู้อนุมัติ-ล็อก + ปุ่ม action ตามสถานะ (ส่งตรวจ/อนุมัติ/ล็อก/ปลดล็อก/ตีกลับ/ลบ/สร้างเวอร์ชันใหม่) + ExportMenu
- **Verify (JSP CONNX 2025):** workflow ครบ (review/final/lock/unlock 200); guards ถูก (ลบ locked→422, locked→draft→422); **snapshot ณ finalize ถูก**: name=LegalName, equity −704,161.20 (ตรง BS/CAP), netProfit 519,308.86, finalizedBy=admin; ปลดล็อก clear lockedBy คง finalizedBy; UI สร้าง→ส่งตรวจ→อนุมัติ เห็น snapshot ผ่าน Preview ไม่มี console error
- **Bug แก้ระหว่าง verify:** unlock (Locked→Final) เดิมไปเข้า branch finalize (re-snapshot, ไม่ clear lock) เพราะลำดับ if — แก้โดยเช็คเงื่อนไข unlock ก่อน finalize
- **gotcha:** ส่ง title ภาษาไทยผ่าน `curl -d` ทำ JSON พัง (ทดสอบผ่านไฟล์ UTF-8 / หรือไม่ใส่ title)

### ✅ Snapshot Express DBF เก็บถาวร 10 ปี — เสร็จ (2026-06-07, req v11 #10/#13, docs/20)
เก็บ **ไฟล์ DBF ต้นฉบับ** ของ Express เป็นหลักฐาน (Import Evidence) ทุกครั้งที่นำเข้า — เพื่อให้ยอดที่ปิดงบแล้วตรวจสอบย้อนกลับได้แม้ Express จะถูกแก้ภายหลัง

- **Domain:** `ImportSnapshot` (1:1 กับ ImportBatch: CapturedAt, SourceFolderPath, ArchiveRelativePath/FileName/ByteSize/Sha256, FileCount, TotalSourceBytes, Status enum `ImportSnapshotStatus` [Captured/Partial/Failed, เก็บ int], Note, **RetainUntil = CapturedAt + 10 ปี**) + `ImportSnapshotFile` (รายไฟล์: TableName, FileName, ByteSize, Sha256, RowCount, SourceModifiedAt). migration `AddImportSnapshots`
- **เก็บไฟล์เป็น zip บน filesystem** (`Import:SnapshotBasePath`, เริ่มต้น `D:\ExpressSnapshots\{ClientCode}\{FiscalYear}\snapshot_*.zip`) — ไฟล์ DBF ใหญ่ไม่เหมาะลง DB blob (ต่างจาก PDF เล็กของ payroll); metadata + checksum ลง DB เป็น evidence log. ฝัง `_manifest.txt` (UTF-8 BOM) ในไฟล์ zip สำหรับผู้สอบบัญชี
- **Service:** `IImportSnapshotService` (Application, คืน pure result) + `ImportSnapshotService` (Infrastructure: enumerate ตาราง Express ที่ระบบใช้ + companion .CDX/.FPT/.DBT, อ่าน FileShare.ReadWrite, SHA-256, row count จาก DBF header ไบต์ 4..8, zip + manifest). ตาราง: ISINFO/ISPRD/GLACC/GLBAL/GLJNLIT/FAMAS/ISVAT/ISTAX/ARMAS/ARTRN/APMAS/APTRN/STMAS/BKMAS/BKTRN
- **Lifecycle ตาม batch:** capture ใน `StartExpressImportCommandHandler` (success block, try/catch แยก ไม่ทำ batch ล้ม) → 1 snapshot/batch; **re-import แทนที่** ลบ snapshot เก่าทั้งไฟล์ zip + row (RemoveSupersededBatchesAsync) และ DeleteImportBatch ลบไฟล์ zip ด้วย — ไม่มี orphan (RetainUntil บันทึกเจตนา 10 ปี กัน auto-purge ก่อนกำหนด)
- **CQRS/API:** `GetImportSnapshotQuery` (metadata + รายไฟล์) / `DownloadImportSnapshotQuery` (bytes). `ImportController` — `GET /import/{id}/snapshot` · `GET /import/{id}/snapshot/download` (application/zip)
- **Frontend:** ปุ่ม "หลักฐาน" ในแถวประวัตินำเข้า (เฉพาะ Success) → `SnapshotModal` (สถานะ/เก็บเมื่อ/เก็บถึง พ.ศ./จำนวนไฟล์/ขนาด/checksum/โฟลเดอร์ต้นทาง + ตารางรายไฟล์ + ปุ่มดาวน์โหลด .zip)
- **Bug แก้ระหว่าง verify (pre-existing):** `ImportPage`/`import.types.ts` ประกาศ `ImportStatus`/`ImportSourceType` เป็น **string union** แต่ API serialize enum เป็น **integer** → status badge ว่าง + ปุ่ม Post/รายละเอียด/หลักฐาน ไม่ขึ้น (เพราะ `status === 'Success'` เป็น false เสมอ). แก้เป็น numeric enum (Success=2 ฯลฯ) ตาม pattern โมดูลอื่น. ดู api-enum-int-serialization
- **Verify (JSIT2016/JSP CONNX 2025 e2e):** import → snapshot 28 ไฟล์ (14 ตาราง × .DBF+.CDX; ISINFO/ISPRD ไม่มี CDX), status Captured, RetainUntil 2036; row count ตรงค่าจริง (GLACC 251, APMAS 345, ISVAT 644, ISTAX 46 ฯลฯ); **ดาวน์โหลด zip → SHA-256 ตรงกับที่เก็บใน DB เป๊ะ**, zip integrity OK, manifest ครบ; re-import → snapshot เก่าถูกแทนที่ ไฟล์ zip เก่าถูกลบ (เหลือ 1 ไฟล์ ไม่มี orphan); UI modal + badge "สำเร็จ" + ปุ่มครบ ผ่าน Preview ไม่มี console error

### ✅ Cash Count + Interest Income — เสร็จ (2026-06-07, req v11 docs/13 §6–7)
กระดาษทำการปิดงบป้อนมือ 2 ตัว (Express ไม่มีทะเบียน) ตาม pattern Prepaid/Leasing → generate adjustment เข้า TB

**Cash Count (ตรวจนับเงินสด):**
- **Domain:** `CashCount` (header: FiscalYear, CountDate, Reference, CashAccountId, Notes/Attachment, IsActive) + `CashCountLine` (Denomination × Quantity = Amount). `AdjustmentSourceType.CashCount=7`
- **CQRS/API:** Create/Update/Delete (validator) + GetCashCounts (list/ปี) / GetCashCount (detail) / GetCashCountWorkpaper (รวมนับจริงต่อบัญชี + เทียบ GL บัญชีเงินสด debit−credit สะสมถึงสิ้นปี → diff ขาด/เกิน) + **GenerateCashCountAdjustment** (ปรับยอดบัญชีเงินสด=ยอดนับจริง คู่กับบัญชีเงินสดขาด/เกิน [counterpart], ดุลเสมอ). `CashCountController` /api/v1/cash-count
- **Frontend:** เมนู "ตรวจนับเงินสด" (icon calculator) `/cash-count` 2 แท็บ (ใบตรวจนับ + form denomination grid 1000..0.25 / กระดาษทำการ + เทียบ GL + generate เลือก counterpart) + ExportMenu
- **Verify (JSP CONNX 2025):** นับ 5,000+300+140=5,440; GL Cash on Hand 118,259 → diff −112,819; generate ADJ Dr เงินสดขาด/Cr เงินสด 112,819 ดุล ✓

**Interest Income (ดอกเบี้ยรับเงินให้กู้ เช่น เงินกู้กรรมการ 22120):**
- **Domain:** `InterestBearingLoan` (AnnualRatePct/SbtRatePct[3]/LocalTaxPctOfSbt[10]/DayCountBasis[365] + InterestReceivableAccountId/InterestIncomeAccountId) + `LoanPrincipalMovement` (Date, Amount signed ±). `AdjustmentSourceType.InterestIncome=8`
- **Engine `InterestIncomeEngine` (pure):** ยอดเงินต้นคงเหลือเปลี่ยนตาม movement → แบ่งช่วงภายในปี [from, nextChange) นับวันไม่ซ้ำ → ดอกเบี้ยช่วง=round(balance×rate×days/basis,2); SBT=ดอกเบี้ย×SbtRate%; ส่วนท้องถิ่น=SBT×LocalTax%
- **CQRS/API:** Create/Update/Delete + GetInterestLoans / GetInterestLoan (detail+segments) / GetInterestWorkpaper (เทียบ GL บัญชีรายได้ดอกเบี้ย movement ในปี credit−debit) + **GenerateInterestAdjustment** (Dr ดอกเบี้ยค้างรับ / Cr รายได้ดอกเบี้ย; SBT/ท้องถิ่นแสดงเพื่อนำส่ง ไม่ลงรายการ). `InterestIncomeController` /api/v1/interest-income
- **Frontend:** เมนู "ดอกเบี้ยรับเงินให้กู้" (icon percent) `/interest-income` 2 แท็บ (เงินให้กู้ + form movement rows + ScheduleModal ช่วงดอกเบี้ย / กระดาษทำการ + เทียบ GL + generate) + ExportMenu
- **Verify (JSP CONNX 2025):** 1,000,000@5% (1ม.ค.) + 500,000 (1ก.ค.) → ช่วง 181 วัน 24,794.52 + 184 วัน 37,808.22 = **62,602.74** (เป๊ะ), SBT 1,878.08, ท้องถิ่น 187.81; generate ADJ Dr 11531/Cr 42100 62,602.74 ดุล ✓
- migration `AddCashCountAndInterestIncome`. ทั้งคู่ enum serialize เป็น int (ดู api-enum-int-serialization)

### ✅ Subsequent Payment Check (RPT-019) — เสร็จ (2026-06-08, req v11 docs/17)
ตรวจรายการ "ค้างจ่าย ณ สิ้นปีปิดงบ" (บัญชีหนี้สิน) ว่าถูกจ่ายชำระจริงในปีถัดไปหรือยัง — เป็น **หลักฐานประกอบการสอบทาน (subsequent evidence) เท่านั้น ไม่นำมารวมยอดปีปิดงบ**

- **สถาปัตยกรรม (สำคัญ):** ระบบ import แบบ annual-aggregated (OPEN/MOVE ทีละปี) ไม่เก็บ GL detail; ปีถัดไป (N+1) ยังไม่ปิด/ยังไม่ import → **อ่าน `GLJNLIT` (สมุดรายวันระดับรายการ) ของ Express "สด"** (ปี N+1 ยังเป็นรอบ current ใน Express → detail ยังอยู่) เหมือนที่ `DeleteImportBatch` อ่าน ISPRD สด — ตรงสเปก "GL1/JV1/PV ปีถัดไป" + หลักการ "ดึงจาก Express ไม่ CRUD ทับ". **เป็น read-only report — ไม่มี entity/migration/adjustment**
- **GLJNLIT schema:** `VOUCHER` `VOUDAT`(วันที่) `ACCNUM`(=Account.AccountCode) `DESCRP` **`TRNTYP` ('' / '0' = เดบิต, '1' = เครดิต** ยืนยันเดบิตรวม=เครดิตรวมเป๊ะ) `AMOUNT`
- **ตรรกะ:** ยอดค้างจ่าย ณ สิ้นปี = บัญชี Liability ที่ลงรายการได้ ซึ่งมียอดสะสมเครดิต > 0 จาก **JournalEntryLines ที่ import แล้ว** (สะสมถึง 31 ธ.ค. ปีงบ); การจ่ายปีถัดไป = เดบิตบัญชีนั้นใน GLJNLIT ปี N+1. จัดประเภท: `paid` (จ่าย ≥ ยอดค้าง) / `partial` (จ่ายบางส่วน) / `unpaid` (ยังไม่พบการจ่าย) / `unmatched` (อ่าน Express ปีถัดไปไม่ได้). **Status เก็บเป็น string** (เลี่ยง int-enum gotcha)
- **โครงสร้าง:** `IExpressDbfAdapter.ReadGlJournalLinesAsync` + DTO `ExpressGlJournalLineDto` · `GetSubsequentPaymentCheckQuery` (inject db + IImportStorageService + IExpressDbfAdapter) · `SubsequentPaymentController` `GET /api/v1/subsequent-payment/check?clientCompanyId&fiscalYear`
- **Frontend:** เมนู "ตรวจจ่ายหลังปิดงบ" (กลุ่มรายงานและปิดงวด, icon bill) `/subsequent-payment` — หน้าเดียว: เลือกปีปิดงบ (default ปีก่อน) + banner ตรวจสด/reference + การ์ดสรุป (จ่ายแล้ว/บางส่วน/ยังไม่พบ) + ตาราง expandable ดูรายการจ่ายรายใบ (วันที่/เลขที่/รายละเอียด/จำนวน) + ExportMenu
- **Verify e2e (JSP CONNX 226, FY2025):** expressAvailable=true อ่าน GLJNLIT 2026 สด, 9 บัญชี — 21110 A/P FG paid (30 รายการ), 21151 Leasing partial (จ่าย 7,824 = 4 งวด HP×1,956), 21433 WHT paid (67,701.75), 21460 Revenue Dept paid, 21416/21491/21492/22120 unpaid (22120 Long-term Loan ไม่จ่ายปีถัดไป = ถูกต้อง). ยอดจ่ายตรงการรวม GLJNLIT ด้วย python เป๊ะ; UI + drill-down ภาษาไทย ผ่าน Preview ไม่มี console error
- **ขอบเขต/หมายเหตุ:** เป็นการจับคู่ **ระดับบัญชี** (account-level) — ยอดจ่ายปีถัดไปอาจรวมการจ่ายของยอดที่ตั้งใหม่ในปีถัดไปด้วย (Express ไม่ผูก payment↔accrual รายใบใน GL) จึงแสดง detail รายใบให้ผู้สอบทานเจาะตรวจเอง; ต้องเปิดให้เข้าถึงโฟลเดอร์ Express ได้ (ถ้าออฟไลน์ → ทุกแถว unmatched)

### ✅ DBD group-code taxonomy — เสร็จ (2026-06-08, req v11 docs/13 §3)
ผังมาตรฐานงบการเงิน (รหัสกลุ่ม DBD/NPAE) เป็น single source of truth ของ RefCode + ความครอบคลุมต่อบริษัท

- **ของเดิมที่มีอยู่แล้ว:** `StatementLine` (master **global** 29 บรรทัด A1/L1/X1... + TXR/TXP, seed ตอน startup, ใช้ร่วมทุกบริษัท) · `AccountStatementMapping` (Account→RefCode ต่อบริษัท) · validate "group code ต้องมีใน master" ใน `UpsertAccountMappingCommandHandler` (เช็ค `StatementLines.Any(RefCode)` → 404) · ตรวจบัญชีตกหล่น (commit af5ef1e)
- **เพิ่มใหม่:** `GetStatementTaxonomyQuery(ClientCompanyId)` → `StatementTaxonomyDto` (master lines + `MappedAccountCount` ต่อบริษัท + TotalLines/UsedLines/MappedAccounts) · `GET /financial-statement/taxonomy`
- **Frontend:** แท็บใหม่ "ผังมาตรฐาน (DBD)" ในหน้างบการเงิน — แสดง 29 บรรทัดจัดกลุ่มตามหมวด (A/L/E/I/X) + badge จำนวนบัญชีที่ map ของบริษัท + การ์ดสรุป + ExportMenu
- **Refactor (สำคัญ):** `MappingTab` เดิมฮาร์ดโค้ด `REF_CODES` (27 รายการ — **ตกหล่น TXR/TXP** + label drift) → เปลี่ยนเป็นดึงจาก `useStatementTaxonomy` (backend master) ทั้ง dropdown และตารางจัดกลุ่ม = **single source of truth** ไม่ drift อีก
- **Verify (JSP CONNX 226):** API คืน 29 บรรทัด, ใช้ 22, map 97 บัญชี (A1=4/X2=34/...); แท็บ UI render กลุ่มถูก + ชื่อไทยเต็มจาก master; mapping dropdown 30 options (เพิ่ม TXR/TXP กลับมา) 5 optgroups; ไม่มี console error
- **หมายเหตุ:** ใช้รหัสภายใน (A1/L1/X1) ตาม workbook sheet "T"; การ map ตรงรหัส DBD e-Filing ทางการ (npae_com-oth_*.xls) ยังไม่ทำ (ไฟล์อ้างอิงไม่อยู่ใน repo)

### ✅ Bank Reconciliation เต็มรูป (กระทบยอดธนาคาร) — เสร็จ (2026-06-08, req v11 docs/17 RPT-015/016)
นำ bank statement เข้าระบบ → จับคู่กับสมุด (BankTransaction จาก BKTRN) → matched/unmatched 2 ฝั่ง + ตรวจยอดปลาย + generate รายการปรับปรุง (ค่าธรรมเนียม/ดอกเบี้ย) เข้า TB

- **รับเข้า 2 ทาง:** (1) **อัปโหลด PDF** auto-detect ธนาคาร → parser เฉพาะ (SCB/KBANK/TTB) (2) **Excel/CSV เทมเพลต** (ดาวน์โหลดได้) — ทุกธนาคาร/สแกน
- **PDF parser (coordinate-based, PdfPig):** อ่าน word+พิกัด → จับบรรทัด (adaptive cluster Y≤3pt) → date token คอลัมน์ซ้าย (x<120, prefix match เผื่อ date+time ติดกัน) → **2 ตัวเลขท้ายแถว = จำนวนเงิน+ยอดคงเหลือ** → ทิศทางจาก **balance-delta** → **balance self-check** (ยอดต้น+Σฝาก−Σถอน=ยอดปลาย) เป็นตัวพิสูจน์ 100%. detect ธนาคารด้วย**ความถี่รูปแบบวันที่** (SCB=/ KBANK=- TTB=. exclusive). **verify จริง 3 ไฟล์ใน reference/bank: SCB 61 แถว, KBANK 51, TTB 15 — self-check ผ่านทั้งหมด** (opening/closing ตรงเป๊ะ)
- **Domain:** `BankStatementImport` (header + ไฟล์ blob + opening/closing + ParsedOk) + `BankStatementLine` (date/withdrawal/deposit/balance/MatchedBankTransactionId/MatchStatus). enums int `BankStatementImportStatus`, `BankLineMatchStatus`. `AdjustmentSourceType.BankReconciliation=9`. migration `AddBankStatementReconciliation`. dep ใหม่: **UglyToad.PdfPig 0.1.9-custom-5**
- **Matcher (pure):** จับคู่ทิศตรง+จำนวนเท่า+วันที่ห่าง≤4วัน greedy 1:1
- **Recon report:** เอกลักษณ์ `statementClosing + unmatchedBookNet = bookClosing + unmatchedStmtNet` → diff/isBalanced. CQRS: ParseBankStatementPreview/UploadBankStatement(+auto-match)/GetBankStatementImports/GetBankReconciliation/Match/Unmatch/Delete/GenerateBankReconciliationAdjustment + GetBankStatementTemplate. `BankReconciliationController` /api/v1/bank-reconciliation
- **Frontend:** แท็บ 3 "กระทบยอด (Reconciliation)" — เลือกบัญชี + อัปโหลด/พรีวิว (banner self-check) + ดาวน์โหลดเทมเพลต + รายการรอบนำเข้า + หน้ากระทบยอด (banner ยอด + 3 ตาราง matched/unmatched-statement/unmatched-book + จับคู่เอง + generate adjustment)
- **Verify e2e (JSP CONNX 226, บัญชี KASIKORN "11"):** อัปโหลด CSV (4 รายการจริง + ค่าธรรมเนียม + ดอกเบี้ย) → auto-match 4 ถูก, unmatched-statement 2, unmatched-book 25, **diff=0.00 balanced=true**; generate → ADJ-2025-0001 (sourceType=9) Dr=Cr 37.45 สมดุล; UI ผ่าน Preview ไม่มี console error
- **ขอบเขต/อนาคต:** GSB/KTB (ฟอนต์ไทยเพี้ยน) + BBL (สแกน) ใช้เทมเพลต Excel/CSV; PDF ต้นฉบับเก็บที่คลังเอกสาร (หมวด BankStatement); ยังไม่ทำ parser เฉพาะ GSB/KTB + จับคู่หลายต่อหนึ่ง

### ✅ TAX engine เต็มรูป (ภาษีเงินได้นิติบุคคล ภ.ง.ด.50) — เสร็จ (2026-06-14, req v11 docs/16 §1)
ต่อยอดจาก ภ.ง.ด.50 เดิม (กรอกภาษีมือ + ประมาณ 20% flat) → **กระดาษทำการคำนวณภาษีจริงจากกำไรทางบัญชี** — คำนวณทั้งหมด (A-017) แยกข้อมูลต้นทาง/ผลลัพธ์ ตรวจสอบได้

- **ลำดับคำนวณ:** กำไรสุทธิทางบัญชีก่อนภาษี (จาก `GetProfitLossQuery`) → + บวกกลับ (B) − หักออก (C) = กำไรสุทธิทางภาษี → − ผลขาดทุนสะสมยกมา (หักไม่เกินกำไร) = เงินได้สุทธิเพื่อเสียภาษี → ภาษีตามอัตรา → − WHT = ค้างชำระ/ขอคืน; ผลขาดทุนยกไป `E = prev − ที่ใช้ / prev + ขาดทุนปีนี้`
- **อัตรา (RateScheme):** **SME ขั้นบันได** (0–300k ยกเว้น / 300k–3M = 15% / >3M = 20%) · **Flat20** (นิติบุคคลทั่วไป) · **Custom** (กำหนดอัตราเอง)
- **Domain:** `TaxComputation` (header/ปี: RateScheme, CustomRatePct, LossBroughtForward, WhtCredit, Note — unique (company,year)) + `TaxAdjustmentLine` (Kind: AddBack=1/Deduction=2, Description, Amount). enum `TaxRateScheme`/`TaxAdjustmentKind` เก็บ int (HasConversion). migration `AddCorporateTaxComputation`
- **Engine:** `CorporateTaxEngine` (pure) — bracket breakdown + loss carry-forward
- **Integration (ปิดลูปงบดุล):** `SaveTaxComputationCommand` คำนวณภาษีด้วยกำไรปัจจุบัน แล้ว **mirror → `FsExternalInput` X4 (=ภาษีที่คำนวณได้) + WHT** → งบดุลลง counterpart TXP/TXR เดิม (กลไกสมดุลไม่ต้องแตะ)
- **CQRS/API:** `GetTaxComputationQuery` (คำนวณสด + warnings ถ้าไม่มี P&L/อัตราไม่ครบ) · `SaveTaxComputationCommand` (upsert header + replace lines + mirror + audit). `CorporateTaxController` `GET/PUT /api/v1/corporate-tax/computation`
- **Frontend:** rework `Pnd50Page` → กระดาษทำการ 2 คอลัมน์ (ซ้าย: แก้รายการบวกกลับ/หักออก + อัตรา + ขาดทุนสะสม + WHT / ขวา: ผลคำนวณ preview สด + ตารางขั้นบันได) + ExportMenu; route `/pnd50` + เมนูภาษีเดิม (ไม่ต้องเพิ่ม)
- **Verify e2e (JSP CONNX 226, FY2025):** กำไรก่อนภาษีปัจจุบัน 510,160.48 → SME = **31,524.07** (15%×210,160.48) ✓; บวกกลับ 89,839.52 → กำไร 600,000 → ภาษี **45,000.00** (live preview + save) ✓; save → X4 mirror 40,524.07 (เคส +100k −40k) **งบดุลสมดุล diff=0.00** ✓; validator Custom ไม่ใส่อัตรา → 422 ✓; UI ผ่าน Preview ไม่มี console error
- **หมายเหตุ/อนาคต:** ผลขาดทุนสะสมติดตามแบบยอดรวม (ยังไม่แยกอายุ 5 ปีรายปี); ตาราง ภ.ง.ด.50 ทางการ (ใบ ภ.ง.ด.50 เต็มฟอร์ม) ยังไม่ทำ — ปัจจุบันเป็นกระดาษทำการ + export ตาราง

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
