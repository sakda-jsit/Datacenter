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
  await page.goto(url, { waitUntil: 'networkidle' })
  await page.waitForTimeout(r.wait ?? 1200)
  if (r.prepare) await r.prepare(page)
  const file = join(OUT, `${r.name}.png`)
  await page.screenshot({ path: file, fullPage: r.full !== false })
  console.log('captured', r.name, '->', file)
  // route ที่ล้างบริษัททิ้ง ต้องตั้งกลับก่อนไป route ถัดไป
  if (r.restoreCompany) await page.evaluate((id) => localStorage.setItem('companyId', String(id)), COMPANY_ID)
}

await browser.close()
console.log('DONE')
