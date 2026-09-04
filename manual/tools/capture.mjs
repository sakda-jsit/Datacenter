// Playwright screenshot capture for the JSP Datacenter user manual.
// Logs in, selects a company, and captures full-page screenshots of the given routes.
//
// Usage: node tools/capture.mjs [group]
//   group = "sample" (default) | "all"
import { chromium } from 'playwright'
import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'

const __dirname = dirname(fileURLToPath(import.meta.url))
const OUT = join(__dirname, '..', 'site', 'assets', 'img')
const BASE = process.env.MANUAL_BASE || 'http://localhost:5199'
const USER = process.env.MANUAL_USER || 'admin'
const PASS = process.env.MANUAL_PASS || 'admin1234'
const COMPANY_ID = Number(process.env.MANUAL_COMPANY_ID || 3) // JSIT2016 = JSP CONNX

// name = output file (name.png); path = route; wait = extra ms; full = full page
const SAMPLE = [
  { name: 'login', path: '/login', auth: false, full: false },
  { name: 'dashboard', path: '/dashboard' },
  { name: 'import', path: '/import' },
  { name: 'import-validation', path: '/import/1/validation' },
  { name: 'trial-balance', path: '/trial-balance' },
]

const groups = { sample: SAMPLE }
const which = process.argv[2] || 'sample'
const routes = groups[which] || SAMPLE

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
  const file = join(OUT, `${r.name}.png`)
  await page.screenshot({ path: file, fullPage: r.full !== false })
  console.log('captured', r.name, '->', file)
}

await browser.close()
console.log('DONE')
