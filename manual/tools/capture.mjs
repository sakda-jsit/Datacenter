// Playwright screenshot capture for the JSP Datacenter user manual.
// Logs in, selects a company, and captures full-page screenshots of the given routes.
//
// Usage: node tools/capture.mjs [name ...]
//   no args = capture every route below; otherwise only the named ones
//   env: MANUAL_BASE, MANUAL_USER, MANUAL_PASS, MANUAL_COMPANY_ID, MANUAL_YEAR
import { chromium } from 'playwright'
import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const __dirname = dirname(fileURLToPath(import.meta.url))
const OUT = join(__dirname, '..', 'site', 'assets', 'img')
const BASE = process.env.MANUAL_BASE || 'http://localhost:5199'
const USER = process.env.MANUAL_USER || 'admin'
const PASS = process.env.MANUAL_PASS || 'admin1234'
const COMPANY_ID = Number(process.env.MANUAL_COMPANY_ID || 226) // JSIT2016 = JSP CONNX (ดู tools/list-companies.mjs)
const YEAR = process.env.MANUAL_YEAR || '2025'

/** หน้ารายงาน (งบทดลอง/แยกประเภท) ไม่ดึงข้อมูลเอง — ต้องตั้งปีแล้วกด "แสดงรายงาน" ก่อนแคป */
async function showReport(page) {
  await page.fill('input[type="number"]', YEAR)
  await page.getByRole('button', { name: 'แสดงรายงาน' }).click()
  await page.waitForTimeout(2500)
}

// name = ไฟล์ผลลัพธ์ (name.png); path = route; wait = หน่วงเพิ่ม (ms); full = แคปทั้งหน้า
const ROUTES = [
  { name: 'login', path: '/login', auth: false, full: false },
  { name: 'dashboard', path: '/dashboard' },
  {
    // โหมดภาพรวมทุกบริษัท = ยังไม่เลือกบริษัท (ล้าง companyId แล้วโหลดใหม่)
    name: 'dashboard-all',
    path: '/dashboard',
    prepare: async (page) => {
      await page.evaluate(() => localStorage.removeItem('companyId'))
      await page.reload({ waitUntil: 'networkidle' })
      await page.waitForTimeout(2000)
    },
    restoreCompany: true,
  },
  { name: 'clients', path: '/clients' },
  { name: 'import', path: '/import' },
  { name: 'import-validation', path: '/import/1/validation' },
  // รายงานที่มีบัญชีเป็นร้อยแถว: แคปแค่ 1 viewport (fullPage จะยาวหมื่นพิกเซล ย่อลงคู่มือแล้วอ่านไม่ออก)
  { name: 'trial-balance', path: '/trial-balance', prepare: showReport, full: false },
  {
    name: 'general-ledger',
    path: '/general-ledger',
    full: false,
    prepare: async (page) => {
      await showReport(page)
      // กางการ์ดบัญชีใบแรกให้เห็นตารางรายการข้างใน (หัวการ์ดคือ button.w-full)
      const firstCard = page.locator('main button.w-full').first()
      if (await firstCard.count()) {
        await firstCard.click()
        await page.waitForTimeout(600)
      }
    },
  },
]

// ── ภ.พ.30 ──
const VAT_YEAR = process.env.MANUAL_VAT_YEAR || '2025'
const MULTI_BRANCH_COMPANY_ID = Number(process.env.MANUAL_MULTIBRANCH_COMPANY_ID || 287) // บริษัทที่มีหลายสาขาใน Express

/** ตั้งปีภาษีที่ select ตัวแรกของหน้า /vat (ปีจะ auto เลือกปีล่าสุดที่มีข้อมูล ถ้าไม่ตั้งเอง) */
async function setVatYear(page) {
  await page.locator('main select').first().selectOption(VAT_YEAR)
  await page.waitForTimeout(1500)
}

async function openVatTab(page, label) {
  await setVatYear(page)
  await page.getByRole('button', { name: label }).click()
  await page.waitForTimeout(2000)
}

ROUTES.push(
  { name: 'vat-report', path: '/vat', prepare: setVatYear, full: false },
  {
    // ใบช่วยกรอกเป็นฟอร์ม ไม่ยาวมาก — แคปเต็มหน้าให้เห็นครบทุกช่องจนถึง "ภาษีสุทธิ"
    name: 'vat-filing',
    path: '/vat',
    prepare: (page) => openVatTab(page, 'ใบกรอก ภ.พ.30 (e-Filing)'),
  },
  {
    name: 'vat-entries',
    path: '/vat',
    full: false,
    prepare: (page) => openVatTab(page, 'รายละเอียดภาษีซื้อ/ขาย'),
  },
  {
    // ตารางแยกสาขาจะขึ้นเฉพาะบริษัทที่มีหลาย DEPCOD ใน Express — แคปเฉพาะการ์ดใบนั้น
    name: 'vat-branches',
    path: '/vat',
    companyId: MULTI_BRANCH_COMPANY_ID,
    element: (page) => page.locator('div.overflow-x-auto').filter({ hasText: 'แยกตามสาขา' }).first(),
    prepare: async (page) => {
      await openVatTab(page, 'ใบกรอก ภ.พ.30 (e-Filing)')
      // ต้องเลือกเดือนที่มีข้อมูลมากกว่า 1 สาขา ไม่งั้นการ์ด "แยกตามสาขา" จะไม่ขึ้น
      await page.locator('main select').nth(1).selectOption(process.env.MANUAL_BRANCH_MONTH || '3')
      await page.waitForTimeout(2000)
    },
  },
  {
    name: 'vat-branch-mapping',
    path: '/vat',
    companyId: MULTI_BRANCH_COMPANY_ID,
    full: false,
    prepare: async (page) => {
      await openVatTab(page, 'ใบกรอก ภ.พ.30 (e-Filing)')
      await page.getByRole('button', { name: 'แมพเลขสาขา' }).click()
      await page.waitForTimeout(1500)
    },
  },
)

// ── กลุ่ม "บัญชี": ลูกหนี้ / เจ้าหนี้ / สินค้าคงคลัง / ธนาคาร ──
/** คลิกแท็บด้วยชื่อที่เห็นบนหน้าจอ */
function tab(label, extra) {
  return async (page) => {
    await page.getByRole('button', { name: label, exact: true }).click()
    await page.waitForTimeout(2000)
    if (extra) await extra(page)
  }
}

ROUTES.push(
  { name: 'ar-aging', path: '/ar', full: false },
  { name: 'ar-invoices', path: '/ar', full: false, prepare: tab('ใบแจ้งหนี้') },
  { name: 'ap-aging', path: '/ap', full: false },
  { name: 'ap-invoices', path: '/ap', full: false, prepare: tab('ใบตั้งหนี้') },
  {
    name: 'stock-valuation',
    path: '/stock',
    full: false,
    prepare: async (page) => {
      await page.locator('main input[type="number"]').first().fill(YEAR)
      await page.waitForTimeout(2000)
    },
  },
  { name: 'stock-items', path: '/stock', full: false, prepare: tab('รายการสินค้า') },
  { name: 'bank-book', path: '/bank-reconciliation', full: false },
  { name: 'bank-accounts', path: '/bank-reconciliation', full: false, prepare: tab('บัญชีธนาคาร') },
  { name: 'bank-recon', path: '/bank-reconciliation', full: false, prepare: tab('กระทบยอด (Reconciliation)') },
)

// ── กลุ่ม "ภาษี": หัก ณ ที่จ่าย / ภ.ง.ด.50 ──
/** ตั้งปีที่ select ตัวแรกของหน้า (หน้าที่ปีเป็น dropdown ของปีที่มีข้อมูล) */
async function setYearSelect(page) {
  await page.locator('main select').first().selectOption(YEAR)
  await page.waitForTimeout(1800)
}

ROUTES.push(
  { name: 'wht-report', path: '/wht', full: false, prepare: setYearSelect },
  {
    name: 'wht-entries',
    path: '/wht',
    full: false,
    prepare: async (page) => {
      await setYearSelect(page)
      await tab('รายละเอียดรายผู้ถูกหัก')(page)
    },
  },
  {
    name: 'pnd50',
    path: '/pnd50',
    prepare: async (page) => {
      await page.locator('main input[type="number"]').first().fill(YEAR)
      await page.getByRole('button', { name: 'แสดงข้อมูล' }).click()
      await page.waitForTimeout(3000)
    },
  },
  {
    name: 'cit50-mapping',
    path: '/pnd50/cit50-mapping',
    full: false,
    prepare: async (page) => {
      await page.locator('main input[type="number"]').first().fill(YEAR)
      await page.getByRole('button', { name: 'แสดงบัญชี' }).click()
      await page.waitForTimeout(3000)
    },
  },
)

// ── กลุ่ม "ภาพรวม": ปฏิทินงาน / งาน-มอบหมายงาน ──
ROUTES.push(
  { name: 'compliance-calendar', path: '/compliance', full: false },
  {
    name: 'compliance-templates',
    path: '/compliance',
    full: false,
    prepare: tab('ตั้งค่างานประจำ'),
  },
  { name: 'tasks-company', path: '/tasks', full: false },
  { name: 'tasks-board', path: '/tasks', full: false, prepare: tab('งานข้ามบริษัท (workboard)') },
)

// ── กลุ่ม "เงินเดือน" (ต้องใช้บริษัทที่มีข้อมูลเงินเดือน) ──
const PAYROLL_COMPANY_ID = Number(process.env.MANUAL_PAYROLL_COMPANY_ID || 242)

ROUTES.push(
  { name: 'payroll-dashboard', path: '/payroll', companyId: PAYROLL_COMPANY_ID, full: false },
  { name: 'payroll-employees', path: '/payroll', companyId: PAYROLL_COMPANY_ID, full: false, prepare: tab('ทะเบียนพนักงาน') },
  { name: 'payroll-runs', path: '/payroll', companyId: PAYROLL_COMPANY_ID, full: false, prepare: tab('งวดเงินเดือน') },
  {
    name: 'payroll-run-grid',
    path: '/payroll',
    companyId: PAYROLL_COMPANY_ID,
    full: false,
    prepare: async (page) => {
      await tab('งวดเงินเดือน')(page)
      await page.getByRole('button', { name: 'เปิด', exact: true }).first().click()
      await page.waitForTimeout(2500)
    },
  },
  { name: 'payroll-year', path: '/payroll', companyId: PAYROLL_COMPANY_ID, full: false, prepare: tab('รายได้ทั้งปี') },
  { name: 'payroll-mapping', path: '/payroll', companyId: PAYROLL_COMPANY_ID, full: false, prepare: tab('แมพบัญชีเงินเดือน') },
)

// ── กลุ่ม "รายงานและปิดงวด" ชุดที่ 1 ──
/** ตั้งปีในช่อง number ตัวแรก แล้วกดปุ่มที่ระบุ (ถ้ามี) */
function setYearThen(btnLabel, year) {
  return async (page) => {
    await page.locator('main input[type="number"]').first().fill(year || YEAR)
    await page.waitForTimeout(500)
    if (btnLabel) {
      await page.getByRole('button', { name: btnLabel, exact: true }).click()
    }
    await page.waitForTimeout(2500)
  }
}

ROUTES.push(
  {
    name: 'adjustments-tb',
    path: '/adjustments',
    companyId: PAYROLL_COMPANY_ID, // 242 มีรายการปรับปรุง
    full: false,
    prepare: setYearThen('แสดงรายงาน'),
  },
  {
    name: 'adjustments-entries',
    path: '/adjustments',
    companyId: PAYROLL_COMPANY_ID,
    full: false,
    prepare: async (page) => {
      await page.locator('main input[type="number"]').first().fill(YEAR)
      await tab('รายการปรับปรุง')(page)
    },
  },
  { name: 'fixed-assets', path: '/fixed-assets', full: false, prepare: setYearThen() },
  { name: 'fixed-assets-workpaper', path: '/fixed-assets', full: false, prepare: async (page) => {
    await setYearThen()(page)
    await tab('กระดาษทำการ + ปรับปรุง')(page)
  } },
  { name: 'leasing', path: '/leasing', full: false, prepare: setYearThen() },
  { name: 'leasing-workpaper', path: '/leasing', full: false, prepare: async (page) => {
    await setYearThen()(page)
    await tab('กระดาษทำการ + ปรับปรุง')(page)
  } },
  { name: 'prepaid', path: '/prepaid', companyId: PAYROLL_COMPANY_ID, full: false, prepare: setYearThen() },
  { name: 'prepaid-workpaper', path: '/prepaid', companyId: PAYROLL_COMPANY_ID, full: false, prepare: async (page) => {
    await setYearThen()(page)
    await tab('กระดาษทำการ + ปรับปรุง')(page)
  } },
)

const only = process.argv.slice(2)
const routes = only.length ? ROUTES.filter((r) => only.includes(r.name)) : ROUTES
if (!routes.length) {
  console.error('ไม่พบ route ที่ระบุ — มีให้เลือก:', ROUTES.map((r) => r.name).join(', '))
  process.exit(1)
}

await mkdir(OUT, { recursive: true })

const browser = await chromium.launch()
const ctx = await browser.newContext({
  viewport: { width: 1440, height: 900 },
  deviceScaleFactor: 2, // crisp screenshots
  locale: 'th-TH',
})
const page = await ctx.newPage()

// 1) login
await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' })
await page.fill('input[type="text"]', USER)
await page.fill('input[type="password"]', PASS)
await page.click('button[type="submit"]')
await page.waitForURL('**/dashboard', { timeout: 20000 })

// 2) select company (persisted in localStorage, read by CurrentCompanyProvider)
await page.evaluate((id) => localStorage.setItem('companyId', String(id)), COMPANY_ID)

// 3) capture each route
for (const r of routes) {
  const url = `${BASE}${r.path}`
  // route ที่ต้องใช้บริษัทอื่น (เช่น ตัวอย่างบริษัทหลายสาขา)
  if (r.companyId) await page.evaluate((id) => localStorage.setItem('companyId', String(id)), r.companyId)
  await page.goto(url, { waitUntil: 'networkidle' })
  await page.waitForTimeout(r.wait ?? 1200)
  if (r.prepare) await r.prepare(page)
  const file = join(OUT, `${r.name}.png`)
  if (r.element) {
    // แคปเฉพาะส่วนที่ต้องการ (เช่น การ์ดใบเดียวกลางหน้ายาว ๆ)
    const el = r.element(page)
    await el.scrollIntoViewIfNeeded()
    await page.waitForTimeout(400)
    await el.screenshot({ path: file })
  } else {
    await page.screenshot({ path: file, fullPage: r.full !== false })
  }
  console.log('captured', r.name, '->', file)
  // route ที่เปลี่ยน/ล้างบริษัท ต้องตั้งกลับก่อนไป route ถัดไป
  if (r.restoreCompany || r.companyId) {
    await page.evaluate((id) => localStorage.setItem('companyId', String(id)), COMPANY_ID)
  }
}

await browser.close()
console.log('DONE')
