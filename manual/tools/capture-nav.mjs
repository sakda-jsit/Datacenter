import { chromium } from 'playwright'
import { fileURLToPath } from 'node:url'
import { dirname, join } from 'node:path'
const __dirname = dirname(fileURLToPath(import.meta.url))
const OUT = join(__dirname, '..', 'site', 'assets', 'img')
const BASE = 'http://localhost:5199'

const browser = await chromium.launch()
const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, deviceScaleFactor: 2, locale: 'th-TH' })
const page = await ctx.newPage()
await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' })
await page.fill('input[type="text"]', 'admin')
await page.fill('input[type="password"]', 'admin1234')
await page.click('button[type="submit"]')
await page.waitForURL('**/dashboard', { timeout: 20000 })
await page.evaluate(() => localStorage.setItem('companyId', '3'))
await page.goto(`${BASE}/dashboard`, { waitUntil: 'networkidle' })
await page.waitForTimeout(800)

// expand the "รายงานและปิดงวด" group to reveal its items
await page.getByRole('button', { name: /รายงานและปิดงวด/ }).click()
await page.waitForTimeout(500)
const aside = page.locator('aside').first()
await aside.screenshot({ path: join(OUT, 'sidebar-expanded.png') })
console.log('captured sidebar-expanded')

await browser.close()
