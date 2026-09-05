// ตรวจคู่มือ (static site): เรนเดอร์ทุกหน้า หา image ที่โหลดไม่ขึ้น ลิงก์ภายในที่ตาย และ JS error
// Usage: node tools/verify-site.mjs
import { chromium } from 'playwright'
import { pathToFileURL } from 'node:url'
import { fileURLToPath } from 'node:url'
import { dirname, join, resolve } from 'node:path'
import { mkdir, readdir, access } from 'node:fs/promises'

const __dirname = dirname(fileURLToPath(import.meta.url))
const site = join(__dirname, '..', 'site')
const OUT = join(__dirname, '..', '_preview')
await mkdir(OUT, { recursive: true })

// index.html + ทุกไฟล์ใน pages/
const PAGES = ['index.html', ...(await readdir(join(site, 'pages'))).filter((f) => f.endsWith('.html')).map((f) => `pages/${f}`)]

const browser = await chromium.launch()
const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 }, deviceScaleFactor: 1 })).newPage()
const pageErrs = []
page.on('pageerror', (e) => pageErrs.push(String(e)))

let problems = 0
for (const rel of PAGES) {
  const file = join(site, rel)
  await page.goto(pathToFileURL(file).href, { waitUntil: 'networkidle' })
  await page.waitForTimeout(400)
  const name = rel.replace(/[\/\\]/g, '-').replace(/\.html$/, '')
  await page.screenshot({ path: join(OUT, `render-${name}.png`), fullPage: true })

  const broken = await page.evaluate(() =>
    Array.from(document.images).filter((i) => !i.complete || i.naturalWidth === 0).map((i) => i.getAttribute('src')))

  // ลิงก์ภายใน (ไม่ใช่ http/mailto/#) ต้องชี้ไปยังไฟล์ที่มีอยู่จริง
  const hrefs = await page.evaluate(() =>
    Array.from(document.querySelectorAll('a[href]'))
      .map((a) => a.getAttribute('href'))
      .filter((h) => h && !/^(https?:|mailto:|#)/.test(h)))
  const deadLinks = []
  for (const href of hrefs) {
    const target = resolve(dirname(file), href.split('#')[0])
    try { await access(target) } catch { deadLinks.push(href) }
  }

  const navCount = await page.evaluate(() => document.querySelectorAll('.sidebar .nav-group a, .sidebar .nav-group span.todo').length)
  const active = await page.evaluate(() => document.querySelectorAll('.sidebar a.active').length)

  const bad = broken.length || deadLinks.length || navCount === 0 || active !== 1
  if (bad) problems++
  console.log(
    `${bad ? 'FAIL' : 'ok  '} ${rel}`,
    `navItems=${navCount} active=${active}`,
    `brokenImages=${JSON.stringify(broken)}`,
    `deadLinks=${JSON.stringify(deadLinks)}`,
  )
}

console.log('pageErrors=', pageErrs)
if (pageErrs.length) problems++
console.log(problems === 0 ? 'ALL OK' : `พบปัญหา ${problems} จุด`)
await browser.close()
process.exit(problems === 0 ? 0 : 1)
