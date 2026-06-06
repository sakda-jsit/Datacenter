# 23 — Payroll / ประกันสังคม / ภาษีเงินเดือน — สถาปัตยกรรม (ร่างเพื่อยืนยัน)

> สถานะ: **v1 — ยืนยันแล้ว (2026-06-06); กำลัง implement P1**
> บริบท: เราเป็น **สำนักงานบัญชี** ทำงานเงินเดือน/ปกส./ภาษี ให้ลูกค้าหลายบริษัท
> มติที่ยืนยันแล้ว: (1) วางสถาปัตยกรรมทั้ง lifecycle ก่อน (2) บทบาทระบบ = **ไฮบริด** (บันทึก + ช่วยคำนวณ/cross-check ตัวเลขที่ลูกค้าส่งมา) (3) ระบบเป็น **ต้นทาง** + ช่วย generate รายการลง Express (Express ไม่มี DBF payroll) (4) เก็บรูปหน้าบัตร ปชช. ในระบบ → ต้องคุม PDPA

---

## 1. หลักการ
- **Single source of truth:** กรอก/นำเข้า payroll detail ครั้งเดียว → ระบบ generate ทุกปลายทาง (สปส.1-10, ภ.ง.ด.1/1ก, 50 ทวิ, กท.20, รายการลง Express) จากชุดเดียว → เลิก rekey ข้ามไฟล์
- **ไฮบริด:** ลูกค้าจ่ายเงินเดือน + ส่ง slip มา → เราบันทึกตัวเลขจริงจาก slip, ระบบ **คำนวณ ปกส./ภาษีให้เป็นตัวเทียบ (cross-check)** แล้ว flag ส่วนต่าง — ไม่บังคับให้ตัวเลขระบบทับของลูกค้า
- **Multi-tenant:** ทุก entity ผูก `ClientCompanyId` (ทะเบียน/งวด/ยื่น แยกตามบริษัทลูกค้า)
- **Status-driven checklist:** ทุกงานรายเดือน/รายปีมีสถานะ + ผู้รับผิดชอบ + วันครบกำหนด → รู้ว่าค้างขั้นไหน
- **Reconciliation เป็น first-class:** payroll detail ↔ Express GL ↔ แบบที่ยื่น ปกส. ↔ ใบเสร็จ จับคู่อัตโนมัติ flag diff
- **PDPA:** ข้อมูลส่วนบุคคล (รูปบัตร, เลขบัตร, เงินเดือนรายคน) เข้าถึงแบบควบคุม + audit ทุกการดู/ดาวน์โหลด

---

## 2. Data Model (entity หลัก)

### 2.1 Employee (พนักงาน — แกนกลาง, per ClientCompany)
`ClientCompanyId, EmployeeCode, NationalId(เลขบัตร), Prefix, FirstName, LastName,
BirthDate, MaritalStatus, Nationality,
Position/Department, StartDate(วันเริ่มงาน), ResignDate?(วันลาออก), EmploymentStatus(Active/Resigned),
SalaryType(Monthly/Daily), BaseSalary, DailyWage?,
SsoNumber?(เลขผู้ประกันตน), SsoHospital(รพ.), SsoStatus(NotEnrolled/Enrolled/Terminated),
TaxId?(ถ้าต่างจากเลขบัตร), TaxAllowanceConfig?(ลดหย่อน — เฟสหลัง)`
→ ใช้ต่อทุก output (เงินเดือน/ปกส./ภาษี/50 ทวิ)

### 2.2 EmployeeDocument (คลังหลักฐาน — PDPA-sensitive)
`EmployeeId, DocType(IdCardFront/SsoEnrollProof/SsoTerminateProof/Slip/Other),
FileName, ContentType, Bytes(หรือ path), EffectiveDate?, UploadedBy, UploadedAt, Note`
→ แทน "แคปหน้าจอลอยๆ": หลักฐานแจ้งเข้า-ออก ปกส. แนบที่นี่ มี audit + ค้นได้

### 2.3 SsoEnrollment (แจ้งเข้า-ออก ปกส.)
`EmployeeId, Type(Enroll/Terminate), EventDate(วันเข้า/ออกจริง), SubmittedDate?(วันแจ้งเว็บ ปกส.),
Status(Pending/Submitted), ProofDocumentId?, SubmittedBy`
→ ขับ checklist onboarding/offboarding ("แจ้งเข้าแล้ว/ยัง")

### 2.4 PayrollRun (งวดเงินเดือนรายเดือน per company)
`ClientCompanyId, Year, Month, Status(Draft/Recorded/ExpensePosted/SsoFiled/Reconciled/Closed),
SlipSourceNote, totals(GrossIncome/SsoEmployee/SsoEmployer/Tax/Net), CreatedBy`

### 2.5 PayrollItem (รายการต่อพนักงานต่องวด) — map ตรงคอลัมน์ salary sheet
รายได้: `Salary, DailyWageDays/DailyWageAmount, HousingAllowance(ค่าที่พัก), FoodAllowance(ค่าอาหาร),
OT(ค่าล่วงเวลา), Diligence(เบี้ยขยัน), Bonus, OtherIncome, GrossIncome`
ปกส.: `SsoWageBase(รายได้ยื่นปกส. = clamp 1,650–15,000), SsoEmployeePct, SsoEmployee(หักจริง),
SsoEmployer, SsoExcess(หักเกิน)`
ภาษี: `WithholdingTax(TAX)`
หัก/สุทธิ: `Absence(ขาดงาน), OtherDeduction, NetPay`
กท.20: `WcfWage(ค่าจ้างฐาน), WcfExcess(ส่วนที่เกิน)`
+ ช่อง **คำนวณระบบ (ไฮบริด):** `SsoCalc, TaxCalc` → เทียบกับที่กรอกจาก slip → `DiffFlag`

### 2.6 SsoMonthlyFiling (ยื่น สปส.1-10 รายเดือน)
`PayrollRunId, EmployeeCount, TotalWage, EmployeeContribution, EmployerContribution, GrandTotal,
SubmittedDate, FormDocumentId?(แบบที่ยื่น), ReceiptDocumentId?(ใบเสร็จ), ReceiptAmount?, ReceiptDate?,
ReconStatus(PayrollMatch/ReceiptMatch flags)`

### 2.7 Pnd1Filing (ภ.ง.ด.1 รายเดือน) / Pnd1kFiling (ภ.ง.ด.1ก รายปี)
`ClientCompanyId, Year, Month?, TotalIncome, TotalTax, SubmittedDate, FormDoc?, ReceiptDoc?`
→ ภ.ง.ด.1ก แนบ 50 ทวิ (40(1)) ต่อพนักงาน — **reuse engine 50 ทวิ ที่มีอยู่** (WhtCertificate)

### 2.8 WorkersCompFundFiling (กท.20 — กองทุนเงินทดแทน รายปี)
`ClientCompanyId, Year, TotalWage, Rate, AssessedAmount, InvoiceDocId?(ใบแจ้งหนี้), ReceiptDocId?,
PaymentStatus(Unpaid/Paid), JvPosted`

### 2.9 PayrollConfig (อัตราตามช่วงเวลา — effective-dated)
`ClientCompanyId?(หรือ global), EffectiveFrom, SsoEmployeePct(3%/5%), SsoWageFloor(1,650), SsoWageCap(15,000),
WcfRate, PitBrackets(JSON ขั้นบันได)` → รองรับ 3%→5% ที่เห็นใน slip จริง

### 2.10 ExpressPostingLink (กระทบยอด/generate ลง Express)
`SourceType(PayrollExpense/SsoReceipt/WcfInvoice/WcfReceipt), SourceId, Year/Month, Amount,
ExpressMatchStatus(Pending/Matched/Diff), ExpressDocNoRef?, Note`
→ ระบบ generate รายการ (ซื้อ→ค่าใช้จ่ายอื่น / JV) + เทียบ GL ที่ import จาก Express

---

## 3. Lifecycle & Checklist (status workflow)

**Onboarding (ต่อพนักงาน):** รับหน้าบัตร+วันเริ่ม → สร้าง Employee + แนบรูปบัตร → SsoEnrollment(Enroll) → แจ้งเว็บ ปกส. → แนบหลักฐาน → Status=Enrolled
**Offboarding:** ResignDate → SsoEnrollment(Terminate) → แจ้ง → แนบหลักฐาน

**รายเดือน (ต่อบริษัท-ต่อเดือน) — checklist:**
1. รับ slip → แนบ + กรอก PayrollItem (detail)
2. ระบบคำนวณ ปกส./ภาษี → ตรวจ diff กับ slip
3. generate/คีย์รายการ Express (ค่าใช้จ่าย) → reconcile กับ GL
4. สรุปยอด → ยื่น สปส.1-10 → แนบแบบ
5. ได้ใบเสร็จ → reconcile (แบบ↔payroll↔ใบเสร็จ) → คีย์ใบเสร็จ Express
6. ภ.ง.ด.1 (ถ้ามีภาษีหัก)

**รายปี:** ภ.ง.ด.1ก + 50 ทวิ ต่อพนักงาน · กท.20 → ใบแจ้งหนี้ → จ่าย → ใบเสร็จ → JV ค้างจ่าย + Express

→ แสดงเป็น dashboard/checklist ต่อบริษัท (ต่อยอดจาก Compliance Calendar)

---

## 4. Reconciliation (3-way, first-class)
ต่อบริษัท-ต่อเดือน เทียบ 3 ฝั่ง flag ส่วนต่าง:
1. **Payroll detail** (ผลรวม PayrollItem) ↔ **Express GL** (ค่าใช้จ่ายเงินเดือน/ปกส. ที่คีย์)
2. **Payroll ปกส.** (Σ SsoEmployee+SsoEmployer) ↔ **แบบ สปส.1-10 ที่ยื่น** ↔ **ใบเสร็จ ปกส.**
3. **ภาษีหัก** ↔ ภ.ง.ด.1
→ หน้า "กระทบยอดเงินเดือน" แสดง ✓/✗ + ตัวเลขส่วนต่าง

---

## 5. Outputs ที่ระบบ generate
| Output | ที่มา | หมายเหตุ |
|---|---|---|
| สปส.1-10 (รายเดือน) | PayrollItem.SsoWageBase | ตามรูปแบบ `2026_CURVE_SSO` |
| กท.20 ก (รายปี) | Σ WcfWage ทั้งปี | ตาม sheet กท.20 ก |
| ภ.ง.ด.1 / 1ก | WithholdingTax | + ไฟล์ยื่น (txt/zip) |
| 50 ทวิ (40(1)) | Employee + รายได้ทั้งปี | **reuse WhtCertificate engine** |
| สลิปเงินเดือน | PayrollItem | optional (ลูกค้าให้มาแล้ว — เก็บแนบ) |
| รายการ Express | Payroll/SSO/WCF | ค่าใช้จ่ายอื่น / JV — generate + reconcile |

---

## 6. PDPA (เก็บรูปบัตร + ข้อมูลส่วนบุคคล)
- รูปบัตร/เอกสารส่วนบุคคล: เก็บใน `EmployeeDocument` (DB blob หรือ secure disk)
- **เข้าถึงแบบควบคุมสิทธิ์** + **audit ทุกการดู/ดาวน์โหลด** (ใช้ AuditService ที่มี)
- ไม่ใส่ข้อมูลส่วนบุคคลใน URL/log; พิจารณา encryption-at-rest (ยืนยันภายหลัง)

---

## 7. แผนสร้าง (phasing)
- **P1 ทะเบียน + คลังหลักฐาน — ✅ DONE (2026-06-06, commit add56dd+e3fc87e):** Employee master + EmployeeDocument(รูปบัตร/หลักฐาน, blob+PDPA audit) + SsoEnrollment (แจ้งเข้า-ออก, แจ้งแล้ว→auto ปรับ SsoStatus). CQRS+controller (/payroll/employees,/documents,/sso-enrollments) + frontend (PayrollPage/EmployeesTab/EmployeeFormModal). กรอกมือ. enum int. verify e2e curl + UI. **ยังไม่ทำ:** import xlsx (กรอกมือตามมติ), checklist dashboard รวม (P6)
- **P2 งวดเงินเดือนรายเดือน — ✅ DONE (2026-06-06):**
  - **P2a อัตรา ปกส./กองทุน (commit d83e9a2/c5b57ba):** `PayrollRateConfig` **ค่ากลางของระบบ** (ไม่แยกบริษัท) effective-dated เปลี่ยนรายเดือนได้ ปรับไม่ย้อนหลัง; อยู่เมนูระบบ `/settings/payroll-rates` (`PayrollRatesController`, global ไม่ผูก company); `PayrollRates.ResolveEffective`
  - **P2b งวด + คำนวณ (commit 76a4f07/1115ba9):** `PayrollRun`(บริษัท+ปี+เดือน,status) + `PayrollItem`(รายได้/หักจาก slip) + `PayrollCalculator` (Gross/Net, ปกส.=clamp(ฐาน,floor,cap)×อัตรา, ภาษีขั้นบันได annualize เป็น **ตัวเทียบ**). CreateRun auto สร้างรายการพนักงาน Active + prefill จากทะเบียน.
  - **P2b-revision: กรอกผ่าน Excel template (commit eecfe43):** ตามมติผู้ใช้ — แทนการกรอกใน grid เว็บ → สร้างงวด → **ดาวน์โหลด Excel template** (รายชื่อพนักงาน + คอลัมน์รายได้/รายการหัก, คีย์=รหัสพนักงานคอลัมน์ A) → กรอกนอกระบบ → **อัปโหลดทับ** (แก้ไข=อัปโหลดใหม่). `IPayrollExcelService` (ClosedXML BuildTemplate/Parse) + GetPayrollRunTemplateQuery + ImportPayrollRunCommand. UI grid เป็น **read-only** + ปุ่มดาวน์โหลด/อัปโหลด + คอลัมน์ "ปกส./ภาษีคำนวณ" ไฮไลต์ diff. verify e2e round-trip: download→กรอก→upload→recompute+cross-check ถูกต้อง
- **P3 ยื่น ปกส.:** SsoMonthlyFiling + generate สปส.1-10 + แนบแบบ/ใบเสร็จ + reconcile
- **P4 Express integration:** generate/กระทบยอดรายการค่าใช้จ่าย+ใบเสร็จกับ GL
- **P5 รายปี:** ภ.ง.ด.1/1ก + 50 ทวิ + กท.20
- **P6 Dashboard/Checklist + Reconciliation รวม**

---

## 8. ข้อยืนยันจาก user (2026-06-06) — ใช้เป็นสเปก
1. **นำเข้าทะเบียน:** ✅ **กรอกมือในระบบ** (ไม่ import xlsx ในเฟสแรก)
2. **slip:** ✅ เฟสแรก **แนบไฟล์ + กรอก detail มือ** (ไม่ auto-parse)
3. **อัตรา ปกส.:** ✅ เพดาน 15,000 / floor 1,650 / 5% (ลด 3% ช่วงปี 68). **ตั้งเป็น master ปรับได้ + แก้แล้วไม่มีผลย้อนหลัง** → `PayrollConfig` **effective-dated** (เลือกอัตราตาม EffectiveFrom ของงวด; เปลี่ยนอัตราใหม่ = เพิ่มแถว effective ใหม่ ไม่ทับของเก่า งวดที่คำนวณไปแล้วคงเดิม)
4. **PIT ขั้นบันได:** ✅ ระบบคำนวณเป็นตัวเทียบ — สูตรมาตรฐาน (เงินได้ทั้งปี − คชจ.50%≤100,000 − ลดหย่อนส่วนตัว 60,000 − ปกส.) ÷ งวด
5. **บัญชี GL ลง Express:** ✅ **mapping table ต่อบริษัท** (เหมือน FA `AssetAccountMapping`)
6. **กระทบยอด Express:** ✅ เทียบกับยอดบัญชีค่าใช้จ่ายเงินเดือน/ปกส. ใน GL ที่ import อยู่แล้ว

> หมายเหตุ effective-dated (ข้อ 3): หลักเดียวกับ "ปรับแล้วไม่ย้อนหลัง" — `PayrollConfig` เก็บหลายแถวตาม `EffectiveFrom`; การคำนวณงวดใด ๆ เลือกแถวที่ EffectiveFrom ≤ วันงวด ล่าสุด. ใช้กับทั้งอัตรา ปกส. และ PIT brackets.
